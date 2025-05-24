using System.Collections.Generic;
using VagabondK.Interface.LSElectric.Abstractions;
using VagabondK.Protocols.LSElectric;

namespace VagabondK.Interface.LSElectric
{
    abstract class DeviceVariableReader<TInterface, TClient> : Dictionary<DeviceVariable, PlcPoint[]>
    {
        protected DeviceVariableReader(TInterface @interface)
        {
            this.@interface = @interface;
        }

        public TInterface @interface;

        protected abstract IReadOnlyDictionary<DeviceVariable, DeviceValue> Read();

        public void ReadAndUpdatePoints()
        {
            var data = Read();
            foreach (var item in data)
                if (TryGetValue(item.Key, out var points))
                    foreach (var point in points)
                        point.SetReceivedValue(item.Value);
        }
    }
}
