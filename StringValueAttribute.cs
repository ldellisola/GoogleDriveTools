using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoogleDrive
{
    public class StringValueAttribute : Attribute
    {
        public string value;

        public StringValueAttribute(string value)
        {
            this.value = value;
        }
    }
}
