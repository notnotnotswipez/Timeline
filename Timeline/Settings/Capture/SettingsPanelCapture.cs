using System.Collections.Generic;

namespace Timeline.Settings.Capture
{
    public class SettingsPanelCapture
    {
        private List<byte> _bytesList = new List<byte>();
        private bool sizeLess = false;
        private byte[] _bytes;
        private int _index;

        public float time;
        
        public SettingsPanelCapture()
        {
            sizeLess = true;
        }
        
        public SettingsPanelCapture(byte[] bytes)
        {
            _bytesList = null;
            _bytes = bytes;
        }

        public SettingsPanelCapture(int size)
        {
            _bytesList = null;
            _bytes = new byte[size];
        }

        public void AddByte(byte b)
        {
            if (sizeLess)
            {
                _bytesList.Add(b);
            }
            else
            {
                _bytes[_index] = b;
                _index++;
            }
        }
        
        public void AddBytes(byte[] bytes)
        {
            foreach (var bytesCurrent in bytes)
            {
                AddByte(bytesCurrent);
            }
        }
        
        public void AddInt(int i)
        {
            AddBytes(System.BitConverter.GetBytes(i));
        }
        
        public void AddFloat(float f)
        {
            AddBytes(System.BitConverter.GetBytes(f));
        }
        
        public bool AddBool(bool b)
        {
            AddBytes(System.BitConverter.GetBytes(b));
            return b;
        }
        
        public int GetInt()
        {
            int i = System.BitConverter.ToInt32(_bytes, _index);
            _index += 4;
            return i;
        }
        
        public float GetFloat()
        {
            float f = System.BitConverter.ToSingle(_bytes, _index);
            _index += 4;
            return f;
        }
        
        public bool GetBool()
        {
            bool b = System.BitConverter.ToBoolean(_bytes, _index);
            _index += 1;
            return b;
        }
        
        public void Complete()
        {
            if (sizeLess)
            {
                _bytes = _bytesList.ToArray();
                _bytesList = null;
            }
            _index = 0;
        }
        
        public byte[] GetBytes()
        {
            return _bytes;
        }
        
    }
}