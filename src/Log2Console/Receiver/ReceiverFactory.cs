using System;
using System.Collections.Generic;
using System.Reflection;

namespace Log2Console.Receiver
{
    public class ReceiverFactory
    {
        private static ReceiverFactory _instance;

        private static readonly string ReceiverInterfaceName = typeof(IReceiver).FullName;

        private ReceiverFactory()
        {
            // Get all the possible receivers by enumerating all the types implementing the interface
            var assembly = Assembly.GetAssembly(typeof(IReceiver));
            var types = assembly.GetTypes();
            foreach (var type in types)
            {
                // Skip abstract types
                if (type.IsAbstract)
                {
                    continue;
                }

                var findInterfaces =
                    type.FindInterfaces((typeObj, o) => typeObj.ToString() == ReceiverInterfaceName, null);
                if (findInterfaces.Length < 1)
                {
                    continue;
                }

                AddReceiver(type);
            }
        }

        public static ReceiverFactory Instance => _instance ?? (_instance = new ReceiverFactory());

        public Dictionary<string, ReceiverInfo> ReceiverTypes { get; } = new Dictionary<string, ReceiverInfo>();

        private void AddReceiver(Type type)
        {
            var info = new ReceiverInfo
            {
                Name = ReceiverUtils.GetTypeDescription(type), Type = type
            };
            if (type != null) ReceiverTypes.Add(type.FullName ?? throw new InvalidOperationException(), info);
        }

        public IReceiver Create(string typeStr)
        {
            IReceiver receiver = null;

            if (ReceiverTypes.TryGetValue(typeStr, out var info))
            {
                receiver = Activator.CreateInstance(info.Type) as IReceiver;
            }

            return receiver;
        }

        public class ReceiverInfo
        {
            public string Name;
            public Type Type;

            public override string ToString() => Name;
        }
    }
}