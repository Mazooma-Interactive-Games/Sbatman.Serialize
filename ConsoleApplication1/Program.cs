using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sbatman.Serialize;
using Sbatman.Serialize.Auto;

namespace ConsoleApplication1
{
    class Program
    {
        class MyClass
        {
            private Int32 _testa;
            private Int16 _testb;
            private UInt64 _testc;
            public Int32 Testa
            {
                get { return _testa; }
                set { _testa = value; }
            }
            public Int16 Testb
            {
                get { return _testb; }
                set { _testb = value; }
            }
            public UInt64 Testc
            {
                get { return _testc; }
                set { _testc = value; }
            }
        }


        static void Main(String[] args)
        {
            Stopwatch s = Stopwatch.StartNew();
            const Int64 count = 1000;
            for (Int64 i = 0; i < count; i++)
            {
                Packet p = new Packet(43);
                p.Add((Int16) 1);
                p.Add((Int32) 2);
                p.Add((Int64) 3);
                p.Add((UInt16) 4);
                p.Add((UInt32) 5);
                p.Add((UInt64) 6);
                p.Add((Single) 7);
                p.Add((Double) 8);
                p.Add(new List<Int32>() {9, 10, 11, 12});
                p.Add(new List<UInt32>() {13, 14, 15, 16});

                Byte[] data = p.ToByteArray();
                p.Dispose();

                Packet q = Packet.FromByteArray(data);
                q.GetObjects();
                q.Dispose();
            }
            s.Stop();


            var c = TypeContract.ConstructTypeContract(typeof(MyClass));
            

            Console.WriteLine(s.Elapsed.TotalMilliseconds / (Double)count);
            Console.ReadKey();


        }
    }
}
