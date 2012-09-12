using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using StructureMap;

namespace MQTT.Types
{
    public static class Factory
    {
        public static T GetInstance<T>()
        {
            return ObjectFactory.GetInstance<T>();
        }

        public static void Initialize(Dictionary<Type, Type> map)
        {
            foreach (Type intface in map.Keys)
            {
                if (!intface.IsInterface)
                {
                    throw new InvalidOperationException("Every key type must be an interface");
                }

                Type mappedType = map[intface];
                if (mappedType.IsInterface || mappedType.IsAbstract)
                {
                    throw new InvalidOperationException("Every concrete type must not be an interface");
                }
            }

            ObjectFactory.Initialize(x =>
            {
                foreach (Type intface in map.Keys)
                {
                    x.For(intface).Use(map[intface]);
                }
            });
        }
    }
}
