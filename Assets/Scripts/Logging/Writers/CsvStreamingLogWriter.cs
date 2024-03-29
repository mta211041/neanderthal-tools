﻿using System.Text;
using NeanderthalTools.Util;

namespace NeanderthalTools.Logging.Writers
{
    public class CsvStreamingLogWriter : BaseStreamingLogWriter
    {
        public CsvStreamingLogWriter(AsyncFileWriter writer) : base(writer)
        {
        }

        protected override void WriteValues(params object[] values)
        {
            for (var i = 0; i < values.Length; i++)
            {
                Write(values[i].ToString());
                if (i + 1 < values.Length)
                {
                    Write(",");
                }
            }

            Write("\n");
        }

        private void Write(string str)
        {
            Write(Encoding.UTF8.GetBytes(str));
        }
    }
}
