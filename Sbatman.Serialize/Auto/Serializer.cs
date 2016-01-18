using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Sbatman.Serialize.Auto
{
    public class Serializer
    {
        readonly List<TypeContract> _CachedTypeContracts = new List<TypeContract>();

        public Serializer(IEnumerable<Type> typeToBeAwareOf = null)
        {
            if (typeToBeAwareOf == null) return;
            foreach (Type type in typeToBeAwareOf) AcceptType(type);
        }

        public Packet Serialize(Object o)
        {
            Packet returnPacket = new Packet(UInt16.MaxValue - 67);

            List<Tuple<Guid, Object>> objectsToSerialize = new List<Tuple<Guid, Object>> { new Tuple<Guid, Object>(Guid.NewGuid(), o) };
            Dictionary<Guid, Object> objectsSerialized = new Dictionary<Guid, Object>();

            while (objectsToSerialize.Count > 0)
            {
                Tuple<Guid, Object> objectBeingSerialized = objectsToSerialize[objectsToSerialize.Count - 1];
                objectsToSerialize.RemoveAt(objectsToSerialize.Count - 1);
                if (objectBeingSerialized.Item2 == null) continue;

                if (objectsSerialized.ContainsValue(objectBeingSerialized.Item2))
                {
                    // returnPacket.Add(objectsSerialized.FirstOrDefault(a => a.Value == objectBeingSerialized.Item2).Key);
                    continue;
                }

                objectsSerialized.Add(objectBeingSerialized.Item1, objectBeingSerialized.Item2);

                TypeContract typeContract = GetTypeContract(objectBeingSerialized.Item2.GetType());

                returnPacket.Add(objectBeingSerialized.Item1);
                returnPacket.Add(typeContract.UID);
                foreach (Tuple<String, PropertyInfo, Packet.ParamTypes> propertyType in typeContract.PropertyTypes)
                {
                    if (propertyType.Item3 == Packet.ParamTypes.AUTOPACK_REFRENCE)
                    {
                        Guid guid = Guid.NewGuid();
                        Object deserializationTarget = propertyType.Item2.GetValue(objectBeingSerialized.Item2);
                        if (deserializationTarget != null) objectsToSerialize.Add(new Tuple<Guid, Object>(guid, deserializationTarget));
                        returnPacket.AddObject(guid);
                    }
                    else if (propertyType.Item3 == Packet.ParamTypes.AUTOPACK_REFRENCE_LIST)
                    {
                        IReadOnlyCollection<Object> deserializationTargets = (IReadOnlyCollection<Object>)propertyType.Item2.GetValue(objectBeingSerialized.Item2);
                        returnPacket.AddObject(deserializationTargets?.Count ?? -1);
                        foreach (Object obj in deserializationTargets)
                        {
                            Guid guid = Guid.NewGuid();
                            if (obj != null) objectsToSerialize.Add(new Tuple<Guid, Object>(guid, obj));
                            returnPacket.AddObject(guid);
                        }
                    }
                    else
                    {
                        returnPacket.AddObject(propertyType.Item2.GetValue(objectBeingSerialized.Item2));
                    }
                }
            }
            return returnPacket;
        }

        public void AcceptType(Type t)
        {
            if (t.GetTypeInfo().GetCustomAttribute(typeof(AutoPacketable)) != null)
            {
                GetTypeContract(t);
            }
            else
            {
                throw new ArgumentException("Type must have the AutoPacketable attribute", nameof(t));
            }
        }

        private TypeContract GetTypeContract(Type t)
        {
            TypeContract typeContract = _CachedTypeContracts.FirstOrDefault(a => a.ClassType == t);
            if (typeContract != null) return typeContract;
            typeContract = TypeContract.ConstructTypeContract(t);
            _CachedTypeContracts.Add(typeContract);
            return typeContract;
        }

        public static object ConvertList(List<object> value, Type type)
        {
            IList list = (IList)Activator.CreateInstance(type);
            foreach (var item in value)
            {
                list.Add(item);
            }
            return list;
        }

        public Object Deserialize(Packet p)
        {
            Object[] dataObjects = p.GetObjects();
            List<Tuple<Guid, Object>> deserializedObjects = new List<Tuple<Guid, Object>>();
            List<Tuple<Object, Guid, PropertyInfo>> pendingRefrences = new List<Tuple<Object, Guid, PropertyInfo>>();
            List<Tuple<List<Object>,Int32,Guid>> pendingListRefrences = new List<Tuple<List<Object>, Int32, Guid>>();
            List<Tuple<Object, List<Object>, PropertyInfo>> pendingLists = new List<Tuple<Object, List<Object>, PropertyInfo>>();

            int datapos = 0;

            while (datapos < dataObjects.Length)
            {
                Guid objectGuid = (Guid)dataObjects[datapos++];
                string typeString = (string)dataObjects[datapos++];
                TypeContract typeContract = _CachedTypeContracts.FirstOrDefault(a => a.UID == typeString);

                Type objectType = typeContract.ClassType;

                object builtObject = Activator.CreateInstance(objectType);

                foreach (Tuple<String, PropertyInfo, Packet.ParamTypes> propertyType in typeContract.PropertyTypes)
                {
                    if (propertyType.Item3 == Packet.ParamTypes.AUTOPACK_REFRENCE)
                    {
                        pendingRefrences.Add(new Tuple<Object, Guid, PropertyInfo>(builtObject, (Guid)dataObjects[datapos++], propertyType.Item2));
                    }
                    else if (propertyType.Item3 == Packet.ParamTypes.AUTOPACK_REFRENCE_LIST)
                    {
                        Int32 objectCount = (Int32)dataObjects[datapos++];
                        List<Object> lst = new List<Object>(objectCount);
                       
                        pendingLists.Add(new Tuple<Object, List<Object>, PropertyInfo>(builtObject,lst, propertyType.Item2));
                        for (int i = 0; i < objectCount; i++)
                        {
                            pendingListRefrences.Add(new Tuple<List<Object>, Int32, Guid>(lst,i, (Guid)dataObjects[datapos++]));
                        }
                    }
                    else
                    {
                        propertyType.Item2.SetValue(builtObject, dataObjects[datapos++]);
                    }
                }
                deserializedObjects.Add(new Tuple<Guid, Object>(objectGuid, builtObject));
            }

            foreach (Tuple<List<Object>, int, Guid> refrence in pendingListRefrences)
            {
                Tuple<Guid, Object> linkedClass = deserializedObjects.FirstOrDefault(a => a.Item1.Equals(refrence.Item3));
                if (linkedClass != null)
                {
                    if (refrence.Item2 >= refrence.Item1.Count)
                    {
                        refrence.Item1.Add(linkedClass.Item2);
                    }
                    else
                    {
                        refrence.Item1[refrence.Item2]=linkedClass.Item2;
                    }
                     
                }
            }

            foreach (Tuple<Object, Guid, PropertyInfo> refrence in pendingRefrences)
            {
                Tuple<Guid, Object> linkedClass = deserializedObjects.FirstOrDefault(a => a.Item1.Equals(refrence.Item2));
                if (linkedClass != null) refrence.Item3.SetValue(refrence.Item1, linkedClass.Item2);
            }

            foreach (Tuple<Object, List<Object>, PropertyInfo> pendingList in pendingLists)
            {
                pendingList.Item3.SetValue(pendingList.Item1, ConvertList(pendingList.Item2, pendingList.Item3.PropertyType));
            }

            return deserializedObjects[0].Item2;
        }

    }
}
