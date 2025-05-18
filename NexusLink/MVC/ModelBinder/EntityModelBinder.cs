using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NexusLink
{
    public class EntityModelBinder : IModelBinder
    {
        private readonly IRepository _repository;

        public EntityModelBinder(IRepository repository)
        {
            _repository = repository;
        }

        public object BindModel(ControllerContext controllerContext, ModelBindingContext bindingContext)
        {
            // Obtiene el ID de la entidad
            var idValue = bindingContext.ValueProvider.GetValue(bindingContext.ModelName + ".Id");
            if (idValue == null) return null;

            int id;
            if (!int.TryParse(idValue.AttemptedValue, out id))
                return null;

            // Determina el tipo de entidad desde el ModelType
            Type entityType = bindingContext.ModelType;

            // Usa reflection para llamar al método genérico GetById
            MethodInfo method = _repository.GetType().GetMethod("GetById");
            MethodInfo genericMethod = method.MakeGenericMethod(entityType);

            // Recupera la entidad
            var entity = genericMethod.Invoke(_repository, new object[] { id });

            // Actualiza propiedades desde los valores del formulario
            var properties = entityType.GetProperties();
            foreach (var prop in properties)
            {
                var propValue = bindingContext.ValueProvider.GetValue(bindingContext.ModelName + "." + prop.Name);
                if (propValue != null)
                {
                    try
                    {
                        object convertedValue = Convert.ChangeType(propValue.AttemptedValue, prop.PropertyType);
                        prop.SetValue(entity, convertedValue);
                    }
                    catch { /* Ignora errores de conversión */ }
                }
            }

            return entity;
        }
    }
}
