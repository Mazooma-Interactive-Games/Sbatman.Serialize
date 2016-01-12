using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Sbatman.Serialize.Auto
{
    public class TypeContract
    {
        private Type _ClassType;
        internal Type ClassType
        {
            get { return _ClassType; }
        }

        private String _UID;
        public String UID
        {
            get { return _UID; }
        }

        private List<Tuple<String, PropertyInfo, Packet.ParamTypes>> _PropertyTypes;
        internal List<Tuple<String, PropertyInfo, Packet.ParamTypes>> PropertyTypes
        {
            get { return _PropertyTypes; }
        }

        public static TypeContract ConstructTypeContract(Type t)
        {
            TypeContract returnContract = new TypeContract
            {
                _ClassType = t,
                _PropertyTypes = new List<Tuple<String, PropertyInfo, Packet.ParamTypes>>()
            };

            List<Byte> identifierList = new List<Byte>();

            List<PropertyInfo> properties = t.GetRuntimeProperties().ToList();

            foreach (PropertyInfo info in properties)
            {
                if (!info.CanRead || !info.CanWrite) continue;
                if (info.GetCustomAttribute(typeof(SkipPack)) != null) continue;
                Packet.ParamTypes packedType = Packet.DetermineParamType(info.PropertyType);
                if (packedType == Packet.ParamTypes.UNKNOWN) continue;
                returnContract._PropertyTypes.Add(new Tuple<String, PropertyInfo, Packet.ParamTypes>(info.Name, info, packedType));
                identifierList.Add((Byte)info.Name[0]);
                identifierList.Add((Byte)((Int32)packedType % 255));
            }
            for (Int32 i = 0; i < identifierList.Count; i++)
            {
                identifierList[i] = (Byte)(identifierList[i] + i % 255);
            }
            returnContract._UID = Convert.ToBase64String(identifierList.ToArray());

            return returnContract;
        }
    }
}
