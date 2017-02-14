using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XData.Data.Components;
using XData.Data.Element;
using XData.Data.Security;

namespace XData.Data.Configuration
{
    public static class ConfigurationCreator
    {
        public static CommonFieldsSetter CreateCommonFieldsSetter()
        {
            return new CommonFieldsSetterFactory().Create();
        }

        public static DbLogSqlProvider CreateDbLogSqlProvider(ElementContext elementContext)
        {
            return new DbLogSqlProviderFactory().Create(elementContext);
        }

        public static ElementValidator CreateElementValidator(ElementContext elementContext)
        {
            return new ElementValidatorFactory().Create(elementContext);
        }

        public static CurrentUser CreateCurrentUser(ElementContext elementContext)
        {
            return new CurrentUserFactory().Create(elementContext);
        }

        public static ElementContext CreateElementContext()
        {
            ElementContextFactory factory = new ElementContextFactoryFactory().Create();
            return factory.Create();
        }

        public static Authorizor CreateAuthorizor(ElementContext elementContext)
        {
            AuthorizorFactory factory = new AuthorizorFactoryFactory().Create();          
            return factory.Create(elementContext);
        }

        public static AuthorizationConfigGetter CreateAuthorizationConfigGetter(ElementContext elementContext)
        {
            AuthorizationConfigGetterFactory factory = new AuthorizationConfigGetterFactoryFactory().Create();
            if (factory == null) return null;

            return factory.Create(elementContext);
        }

        public static SpecifiedConfigGetter CreateSpecifiedConfigGetter(ElementContext elementContext)
        {
            SpecifiedConfigGetterFactory factory = new SpecifiedConfigGetterFactoryFactory().Create();
            if (factory == null) return null;

            return factory.Create(elementContext);
        }

        public static DataSourceConfigGetter CreateDataSourceConfigGetter(ElementContext elementContext)
        {
            DataSourceConfigGetterFactory factory = new DataSourceConfigGetterFactoryFactory().Create();
            if (factory == null) return null;

            return factory.Create(elementContext);
        }

        public static DataSource CreateDataSource(ElementContext elementContext, SpecifiedConfigGetter specifiedConfigGetter)
        {
            DataSourceFactory factory = new DataSourceFactoryFactory().Create();
            return factory.Create(elementContext, specifiedConfigGetter);
        }

        private static HasherManager HasherManager = new HasherManager();
        public static HasherManager Hashers
        {
            get
            {
                return HasherManager;
            }
        }

        private static CryptorManager CryptorManager = new CryptorManager();
        public static CryptorManager Cryptors
        {
            get
            {
                return CryptorManager;
            }
        }

        public static IEncryptor CreatePasswordEncryptor()
        {
            ServerConfigurationSection configurationSection = (ServerConfigurationSection)ConfigurationManager.GetSection("element.server");
            PasswordConfigurationElement configurationElement = configurationSection.Password as PasswordConfigurationElement;
            string format = configurationElement.Format;
            if (format == "Clear") return null;
            string algorithmName = configurationElement.AlgorithmName;
            if (format == "Hashed")
            {
                return Hashers[algorithmName];
            }
            if (format == "Encrypted")
            {
                return Cryptors[algorithmName];
            }

            throw new NotSupportedException(format);
        }


    }
}
