using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using Netlyt.Interfaces;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Donut.Data.Format
{ 
    public class JsonFormatter<T> : InputFormatter<T>
        where T : class
    {
        public LineMode LineMode { get; set; }

        private JsonSerializer _serializer;
        private JsonTextReader _jsReader;
        private StreamReader _reader;
        private long _position = -1;
        public JsonFormatter()
        {
            _serializer = new JsonSerializer();
        }

        private void ResetReader(Stream fs)
        {
            var streamReader = new StreamReader(fs);
            _jsReader = new JsonTextReader(streamReader);
            Name = "Json";
        }
        /// <summary>
        /// Gets the structure of the input stream
        /// </summary>
        /// <param name="fs"></param>
        /// <returns></returns>
        public override IEnumerable<dynamic> GetIterator(Stream fs, bool resetRead = false, Type targetType = null)
        {
            return GetIterator(fs, resetRead);
        }

        /// <summary>
        /// Gets the structure of the input stream.
        /// Uses the first available object, as structure, assuming each of your objects has the same structure.
        /// </summary>
        /// <typeparam name="T">The type to which to cast the input object</typeparam>
        /// <param name="fs"></param>
        /// <returns></returns>
        public override IEnumerable<T> GetIterator(Stream fs, bool resetRead = false)
        {
            while (true)
            {
                if (!fs.CanRead)
                {
                    yield break;
                }
                _reader = (!resetRead && _reader != null) ? _reader : new StreamReader(fs);
                _jsReader = (!resetRead && _jsReader != null) ? _jsReader : new JsonTextReader(_reader);
                switch (LineMode)
                {
                    case LineMode.EntryPerLine:

                        string nextLine = _reader.ReadLine();
                        T json = JsonConvert.DeserializeObject<T>(nextLine);
                        _position++;
                        base.FormatFields(json as ExpandoObject);
                        yield return json;
                        break;
                    case LineMode.None:
                        //var startedObject = false;
                        JObject obj = null;
                        int startedDepth = 0;

                        while (_jsReader.Read())
                        {
                            if (_jsReader.TokenType == JsonToken.StartObject)
                            {
                                startedDepth = _jsReader.Depth;
                                //startedObject = true;
                                // Load each object from the stream and do something with it 
                                obj = JObject.Load(_jsReader);
                                if (fs.CanSeek)
                                {
                                    _jsReader.Skip();
                                }
                                _position++;
                                var value = obj == null ? default(T) : obj.ToObject<T>();
                                base.FormatFields(value as ExpandoObject);
                                yield return value;
                            }
                            //                        } else if (startedObject && jsonReader.TokenType == JsonToken.EndObject && startedDepth == jsonReader.Depth)
                            //                        {
                            //                            jsonReader = jsonReader;
                            //                            return obj==null ? default(T) : obj.ToObject<T>(); 
                            //                        }
                        }
                         yield break;
                    default:
                        throw new NotImplementedException("Not yet supported!");
                }
            }
        }

        public override IInputFormatter Clone()
        {
            var jsf = new JsonFormatter<T>();
            return jsf;
        }

        public override void Reset()
        {
            throw new NotImplementedException();
        }

        public override long Position()
        {
            return _position;
        }

        public double Progress => 0;

        public override void Dispose()
        {
            ((IDisposable) _jsReader)?.Dispose();
            _reader?.Dispose();
        }
    }
}
