using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure
{
    public static class IoC
    {
        private static Dictionary<Type, object> models = new Dictionary<Type, object>();

        public static void AddModel<T>()
            where T: new()
        {
            models[typeof(T)] = new T();
        }

        public static T GetModel<T>()
        {
            return (T)models[typeof(T)];
        }
    }
}
