using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Imposter.Fiddler.Model
{
    [DataContract]
    public class Override
    {
        [DataMember(Name = "remoteFile")]
        public string RemoteFile { get; set; }

        [DataMember(Name = "localFile")]
        public string LocalFile { get; set; }

        public override string ToString()
        {
            return RemoteFile;
        }
    }
}
