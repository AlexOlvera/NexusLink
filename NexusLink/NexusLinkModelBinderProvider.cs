using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NexusLink
{
    public class NexusLinkModelBinderProvider : IModelBinderProvider
    {
        public IModelBinder GetBinder(Type modelType)
        {
            // Verifica si el tipo está decorado con TableAttribute
            var hasTableAttribute = modelType.GetCustomAttributes(typeof(TableAttribute), true).Any();

            if (hasTableAttribute)
            {
                // Usa IoC para obtener el repositorio apropiado
                var repository = DependencyResolver.Current.GetService<IRepository>();
                return new EntityModelBinder(repository);
            }

            return null;
        }
    }
}
