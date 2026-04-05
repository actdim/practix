using Autofac;
using CanarySystems.Configuration;
using CanarySystems.Configuration.Abstract;
using CanarySystems.Misc;
using CanarySystems.MLConnector.Context;
using CanarySystems.MLConnector.Context.Abstract;
using CanarySystems.MLConnector.Services;
using CanarySystems.Security.Concrete;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;
using System;
using System.IO;
using System.Reflection;

namespace Misc.Tests
{

    [TestFixture]
    public partial class TestContainer
    {
        protected IContainer Container;

        protected string DistributedCachePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "cache.db");

        [SetUp]
        public void SetUp()
        {
            var builder = new ContainerBuilder();
            builder.RegisterModule<MLUserContextPoolModule>()
                .RegisterModule<ConfigurationModule>()
                .RegisterModule<MiscModule>()
                .RegisterModule<MLUserSecurityModule>()
                .RegisterModule<AutoMapperModule>()                

            // var configurationBuilder = new ConfigurationBuilder();
            // var configuration = configurationBuilder
            //	.AddJsonFile("cache.config")
            //	.AddEnvironmentVariables()
            //	.Build();

            // configuration["connectionString"]...

            // {
            //  "connectionString": "Filename=cache.db",
            //  "tableName": "cache"
            // }

            builder.RegisterType<SQLiteCache>()
               .As<IDistributedCache>()
               .WithParameter(
                    (pi, ctx) => pi.ParameterType == typeof(IOptions<SQLiteCacheOptions>),
                    (pi, ctx) =>
                        // ctx.Resolve<IOptions<SQLiteCacheOptions>>()
                        new SQLiteCacheOptions()
                        {
                            SystemClock = new SystemClock(),
                            // In-Memory mode:
                            // cache.db?mode=memory&cache=shared
                            // PRAGMA auto_vacuum=1;PRAGMA synchronous=0;PRAGMA cache_size=4000;PRAGMA journal_mode=WAL
                            ConnectionString = $"Data Source={DistributedCachePath};New=True;Version=3;Pooling=True;Max Pool Size=100;",
                            TableName = "cache",
                            // DefaultSlidingExpiration = TimeSpan.FromMinutes(1),
                            ExpiredItemsDeletionInterval = TimeSpan.FromSeconds(1)
                        }
                    )
                // .SingleInstance()
                .InstancePerLifetimeScope();


#if MOCK_MLSERVER
            RegisterMocks(builder);
#else
            builder.RegisterInstance<ISuiteConfiguration>(Config.Value);
#endif
            Container = builder.Build();
        }

#if MOCK_MLSERVER
        protected bool preventCheckAuth = false;
#endif

        private void RegisterMocks(ContainerBuilder builder)
        {
            var mContext = new Mock<IMLUserContext>();
            mContext.Setup(x => x.GetTime()).Callback(() =>
            {
            });
            var mFactory = new Mock<IMLUserContextFactory>();
            mFactory.Setup(x => x.CreateUserContext(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Callback((string host, string port, string user, string password) =>
                {
#if MOCK_MLSERVER
                    if (preventCheckAuth)
                    {
                        return;
                    }
#endif
                    if (user != "sysdba@bagdad" || password != "masterkey")
                    {
                        throw new MLAuthorizeException("invalid username/password", null, null);
                    }
                })
                .Returns(mContext.Object);
            builder.RegisterInstance(mFactory.Object).As<IMLUserContextFactory>();
            var mConfiguration = new Mock<ISuiteConfiguration>();
            mConfiguration.SetupGet(x => x.MLServerHost).Returns("test");
            mConfiguration.SetupGet(x => x.MLServerPort).Returns("9009");
            builder.RegisterInstance(mConfiguration.Object).As<ISuiteConfiguration>();
        }
        protected static Lazy<TestSuiteConfig> Config = new(() =>
        {
            var asm = Assembly.GetExecutingAssembly();
            var fileName = asm.GetName().Name + ".json";
            var path = Path.Combine(Path.GetDirectoryName(asm.Location), fileName);

            var builder = new ConfigurationBuilder()
                .SetBasePath(TestContext.CurrentContext.TestDirectory)
                .AddEnvironmentVariables()
                .AddJsonFile(path, optional: false)
                .AddCommandLine(Environment.GetCommandLineArgs());

            var config = builder.Build();
            var testConfig = config.Get<TestSuiteConfig>();
            path = testConfig.DataPath;
            if (string.IsNullOrEmpty(path))
            {
                path = FindFolder(DataFolderName);
                testConfig.DataPath = path;
            }
            path = testConfig.PdalExePath;
            if (string.IsNullOrEmpty(path))
            {
                path = ResolvePath("pdal.exe", FindFolder("3rdParty\\PDAL"));
                testConfig.PdalExePath = path;
            }
            path = testConfig.TempFolder;
            if (string.IsNullOrEmpty(path))
            {
                path = Path.GetTempPath();
                testConfig.TempFolder = path;
            }
            testConfig.CanarySysFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonDocuments), "CanarySys", "MLSuite");
            return testConfig;
        });

        protected static string ResolvePath(string path = "", string basePath = default)
        {
            if (string.IsNullOrEmpty(basePath))
            {
                basePath = Config.Value.DataPath;
            }
            path = Path.GetFullPath(Path.Combine(basePath, path));
            TestContext.WriteLine($"Resolved file: {path}");
            return path;
        }

        public const string DataFolderName = "3rdParty\\TestData";

        public static string FindFolder(string folderPath, string basePath = default)
        {
            if (string.IsNullOrEmpty(basePath))
            {
                basePath = TestContext.CurrentContext.TestDirectory;
            }
            var currentDir = new DirectoryInfo(basePath);
            while (currentDir != null)
            {
                var path = Path.Combine(currentDir.FullName, folderPath);
                if (Directory.Exists(path))
                {
                    return path;
                }
                currentDir = currentDir.Parent;
            }
            return default;
        }
    }
}
