using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel;
using BookStore.Services;
using System.ServiceModel.Configuration;
using System.Configuration;
using System.ComponentModel.Composition.Hosting;
using BookStore.Services.Interfaces;
using Microsoft.Practices.ServiceLocation;
using Microsoft.Practices.Unity.ServiceLocatorAdapter;
using Microsoft.Practices.Unity;
using Microsoft.Practices.Unity.Configuration;
using BookStore.Business.Entities;
using System.Transactions;
using System.ServiceModel.Description;
using BookStore.Business.Components.Interfaces;
using BookStore.WebClient.CustomAuth;
using System.Messaging;

namespace BookStore.Process
{
    public class Program
    {
        private static readonly String sPublishQueuePath = ".\\private$\\BankNotificationQueueTransacted";
        private static readonly String sPublishQueuePath2 = ".\\private$\\DeliveryNotificationQueueTransacted";

        static void Main(string[] args)
        {
            EnsureQueueExists(sPublishQueuePath);
            EnsureQueueExists(sPublishQueuePath2);
            ResolveDependencies();
            InsertDummyEntities();
            HostServices();
        }

        private static void EnsureQueueExists(String sPublishQueuePath)
        {
            // Create the transacted MSMQ queue if necessary.
            if (!MessageQueue.Exists(sPublishQueuePath))
                MessageQueue.Create(sPublishQueuePath, true);
        }

        private static void InsertDummyEntities()
        {
            InsertCatalogueEntities();
            CreateOperator();
            CreateUser();
        }

        private static void CreateUser()
        {
            using (BookStoreEntityModelContainer lContainer = new BookStoreEntityModelContainer())
            {
                if (lContainer.Users.Where((pUser) => pUser.Name == "Customer").Count() > 0)
                    return;
            }

           
            User lCustomer = new User()
            {
                Name = "Customer",
                LoginCredential = new LoginCredential() { UserName = "Customer", Password = "COMP5348" },
                Email = "David@Sydney.edu.au",
                Address = "1 Central Park",
                BankAccountNumber = 456,
            };

            ServiceLocator.Current.GetInstance<IUserProvider>().CreateUser(lCustomer);
        }

        private static void InsertCatalogueEntities()
        {
            using (TransactionScope lScope = new TransactionScope())
            using (BookStoreEntityModelContainer lContainer = new BookStoreEntityModelContainer())
            {
                if (lContainer.Books.Count() == 0)
                {

                    List<Warehouse> warehouses = new List<Warehouse>();

                    String[] names = { "Neutral Bay", "Camperdown", "Five Dock", "CBD", "Newtown" };

                    for (int i = 0; i < 5; i ++)
                    {
                        Warehouse lWarehouse = new Warehouse()
                        {
                            Id = i,
                            Name = names[i]
                        };
                        lContainer.Warehouses.Add(lWarehouse);
                        warehouses.Add(lWarehouse);
                    }


                    Book lGreatExpectations = new Book()
                    {
                        Author = "Jane Austen",
                        Genre = "Fiction",
                        Price = 20.0,
                        Title = "Pride and Prejudice"
                    };

                    lContainer.Books.Add(lGreatExpectations);

                    Stock lGreatExpectationsStock = new Stock()
                    {
                        Book = lGreatExpectations,
                        Quantity = 5,
                        Warehouse = warehouses[1]
                    };

                    lContainer.Stocks.Add(lGreatExpectationsStock);

                    Stock lGreatExpectationsStock1 = new Stock()
                    {
                        Book = lGreatExpectations,
                        Quantity = 10,
                        Warehouse = warehouses[3]
                    };

                    lContainer.Stocks.Add(lGreatExpectationsStock1);

                    Book lSoloist = new Book()
                    {
                        Author = "Charles Dickens",
                        Genre = "Fiction",
                        Price = 15.0,
                        Title = "Grape Expectations"
                    };

                    lContainer.Books.Add(lSoloist);

                    Stock lSoloistStock = new Stock()
                    {
                        Book = lSoloist,
                        Quantity = 7,
                        Warehouse = warehouses[3]
                    };

                    lContainer.Stocks.Add(lSoloistStock);

                    Stock lSoloistStock1 = new Stock()
                    {
                        Book = lSoloist,
                        Quantity = 7,
                        Warehouse = warehouses[4]
                    };

                    lContainer.Stocks.Add(lSoloistStock1);

                    Random random = new Random();

                    for (int i = 1; i < 10; i++)
                    {
                        Book lItem = new Book()
                        {
                            Author = String.Format("Author {0}", i.ToString()),
                            Genre = String.Format("Genre {0}", i),
                            Price = i,
                            Title = String.Format("Title {0}", i)
                        };

                        List<int> usedWarehouses = new List<int>();

                        for (int j = 0; j < random.Next(2, 5); j ++)
                        {
                            int warehouse = random.Next(0, 4);
                            if (!usedWarehouses.Contains(warehouse))
                            {
                                usedWarehouses.Add(warehouse);
                                Stock tempStock = new Stock()
                                {
                                    Book = lItem,
                                    Quantity = random.Next(1, 20),
                                    Warehouse = warehouses[warehouse]
                                };
                                lContainer.Stocks.Add(tempStock);
                            }
                        }

                    }

                    lContainer.SaveChanges();
                    lScope.Complete();
                }
            }
        }

   

        private static void CreateOperator()
        {
            Role lOperatorRole = new Role() { Name = "Operator" };
            using (BookStoreEntityModelContainer lContainer = new BookStoreEntityModelContainer())
            {
                if (lContainer.Roles.Count() > 0)
                {
                    return;
                }
            }
            User lOperator = new User()
            {
                Name = "Operator",
                LoginCredential = new LoginCredential() { UserName = "Operator", Password = "COMP5348" },
                Email = "Wang@Sydney.edu.au",
                Address = "1 Central Park"
            };

            lOperator.Roles.Add(lOperatorRole);

            ServiceLocator.Current.GetInstance<IUserProvider>().CreateUser(lOperator);
        }

        private static void ResolveDependencies()
        {

            UnityContainer lContainer = new UnityContainer();
            UnityConfigurationSection lSection
                    = (UnityConfigurationSection)ConfigurationManager.GetSection("unity");
            lSection.Containers["containerOne"].Configure(lContainer);
            UnityServiceLocator locator = new UnityServiceLocator(lContainer);
            ServiceLocator.SetLocatorProvider(() => locator);
        }


        private static void HostServices()
        {
            List<ServiceHost> lHosts = new List<ServiceHost>();
            try
            {

                Configuration lAppConfig = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                ServiceModelSectionGroup lServiceModel = ServiceModelSectionGroup.GetSectionGroup(lAppConfig);

                System.ServiceModel.Configuration.ServicesSection lServices = lServiceModel.Services;
                foreach (ServiceElement lServiceElement in lServices.Services)
                {
                    ServiceHost lHost = new ServiceHost(Type.GetType(GetAssemblyQualifiedServiceName(lServiceElement.Name)));
                    lHost.Open();
                    lHosts.Add(lHost);
                }
                Console.WriteLine("BookStore Service Started, press Q key to quit");
                while (Console.ReadKey().Key != ConsoleKey.Q) ;
            }
            finally
            {
                foreach (ServiceHost lHost in lHosts)
                {
                    lHost.Close();
                }
            }
        }

        private static String GetAssemblyQualifiedServiceName(String pServiceName)
        {
            return String.Format("{0}, {1}", pServiceName, System.Configuration.ConfigurationManager.AppSettings["ServiceAssemblyName"].ToString());
        }
    }
}
