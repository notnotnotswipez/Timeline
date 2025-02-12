using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Timeline.Serialization.Binary;

namespace Timeline.Serialization
{
    public abstract class SerializableMember
    {

        public abstract byte SerializeableID { get; }

        public abstract int GetSize();

        public abstract void WriteToStream(BinaryStream stream);
        public abstract void ReadFromStream(BinaryStream stream);
    }
}
