#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Net.Http;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;

namespace Pico.AvatarAssetBuilder
{
    /// <summary>
    /// Content-Type
    /// </summary>
    public enum ContentType
    {
        Json,
        Multipart
    }

    public class CommonHttpRequest
    {
        internal string host;
        internal string path;
        internal string url;

        [JsonIgnore] internal HttpMethod httpMethod;
        internal ContentType contentType;

        internal IDictionary<string, string> queryParams;
        private IDictionary<string, object> postFields;
        internal IDictionary<string, string> headers;

        internal string postDataJson;
        internal string filePath;

        private bool useUnityWebRequest;

        public UnityWebRequest CurrRequest;
        private CommonHttpRequest()
        {
        }

        /// <summary>
        ///  Network request entry
        /// </summary>
        /// <param name="success"></param>
        /// <param name="failure"></param>
        public void Send(Action<string> success, Action<NetworkFailure> failure)
        {
            Debug.Log("HttpRequest send request " + this.ToString());
            if (useUnityWebRequest)
            {
                EditorWebRequest.Send(this, success, failure);
                return;
            }
#if UNITY_EDITOR
            EditorWebRequest.Send(this, success, failure);
#else
            //1. use host bridge net impl
            //2. use sdk inner c++ impl
#endif
        }
        
        /// <summary>
        ///  Network request entry
        /// </summary>
        /// <param name="success"></param>
        /// <param name="failure"></param>
        public void Send(Action<float> progress, Action<string> success, Action<NetworkFailure> failure)
        {
            Debug.Log("HttpRequest send request " + this.ToString());
            if (useUnityWebRequest)
            {
                EditorWebRequest.Send(this, progress, success, failure);
                return;
            }
#if UNITY_EDITOR
            EditorWebRequest.Send(this, progress, success, failure);
#else
            //1. use host bridge net impl
            //2. use sdk inner c++ impl
#endif
        }
        
        /// <summary>
        /// Start Download file
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="success"></param>
        /// <param name="progress"></param>
        /// <param name="failure"></param>
        public void StartDownload(string fileName, Action<string> success, Action<string, float> progress,
            Action<string, string> failure)
        {
            Debug.Log("StartDownload send request " + this.ToString());
            if (useUnityWebRequest)
            {
                EditorWebRequest.StartDownload(this, fileName, success, progress,failure);
                return;
            }
#if UNITY_EDITOR
            EditorWebRequest.StartDownload(this, fileName, success, progress,failure);
#else
            //1. use host bridge net impl
            //2. use sdk inner c++ impl
#endif
        }

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }

        public class Builder
        {
            /// <summary>
            /// request host
            /// </summary>
            private string _host;

            /// <summary>
            /// request path
            /// </summary>
            private string _path;
            
            /// <summary>
            /// request URL
            /// </summary>
            private string _url;

            /// <summary>
            /// request Method
            /// </summary>
            [JsonIgnore] private HttpMethod _httpMethod;

            /// <summary>
            /// request Content-Type
            /// </summary>
            private ContentType _contentType;

            /// <summary>
            /// request params
            /// </summary>
            private IDictionary<string, string> _queryParams;

            /// <summary>
            /// request post filed
            /// </summary>
            private IDictionary<string, object> _postFields;

            /// <summary>
            /// request headers
            /// </summary>
            private IDictionary<string, string> _headers;

            /// <summary>
            /// request post Json
            /// </summary>
            private string _postDataJson;

            /// <summary>
            /// request file Path
            /// </summary>
            private string _filePath;

            /// <summary>
            /// whether use UnityWebRequest
            /// </summary>
            private bool _useUnityWebRequest;

            public Builder SetHostForEditor(string host)
            {
                _host = host;
                return this;
            }

            public Builder SetHttpMethod(HttpMethod method)
            {
                _httpMethod = method;
                return this;
            }
            
            public Builder SetHttpUrl(string url)
            {
                _url = url;
                return this;
            }

            public Builder SetContentType(ContentType contentType)
            {
                _contentType = contentType;
                return this;
            }

            public Builder SetPath(string path)
            {
                if (!path.StartsWith("/"))
                {
                    throw new ArgumentException("relative path must start with /");
                }

                if (path.EndsWith("/"))
                {
                    throw new ArgumentException("relative path cannot end with /");
                }

                _path = path;
                return this;
            }

            public Builder AddQueryParam(string key, object value)
            {
                _queryParams ??= new Dictionary<string, string>();
                CheckSimpleType(value);
                _queryParams[key] = NormalizeString(value);
                return this;
            }

            public Builder SetQueryParams(Dictionary<string, string> queryParams)
            {
                _queryParams = queryParams;
                return this;
            }

            public Builder SetPostData(string json)
            {
                _postDataJson = json;
                return this;
            }

            public Builder SetUploadFilePath(string filePath)
            {
                _filePath = filePath;
                return this;
            }

            public Builder AddPostField(string key, object value)
            {
                _postFields ??= new Dictionary<string, object>();
                CheckSimpleType(value);
                _postFields[key] = value;
                return this;
            }

            public Builder AddHeader(string key, object value)
            {
                _headers ??= new Dictionary<string, string>();
                CheckSimpleType(value);
                _headers[key] = NormalizeString(value);
                return this;
            }

            public Builder SetHeaders(IDictionary<string, string> header)
            {
                _headers = header;
                return this;
            }

            public Builder DebugWithUnityWebRequest(bool debug)
            {
                _useUnityWebRequest = debug;
                return this;
            }

            public CommonHttpRequest Build()
            {
                if (_httpMethod == null)
                {
                    throw new ArgumentException("http method is null");
                }

                var request = new CommonHttpRequest
                {
                    host = _host,
                    path = _path,
                    url = _url,
                    httpMethod = _httpMethod,
                    contentType = _contentType,
                    queryParams = _queryParams,
                    headers = _headers,
                };

                if (_httpMethod == HttpMethod.Post)
                {
                    if (!string.IsNullOrEmpty(_postDataJson))
                    {
                        request.postDataJson = _postDataJson;
                    }
                    else if (_postFields != null && _postFields.Count > 0)
                    {
                        request.postDataJson = JsonConvert.SerializeObject(_postFields);
                    }
                    else if (!string.IsNullOrEmpty(_filePath))
                    {
                        request.filePath = _filePath;
                    }
                    else
                    {
                        // throw new ArgumentException("empty post data for " + _path);
                    }
                }

                request.useUnityWebRequest = _useUnityWebRequest;

                return request;
            }

            private static void CheckSimpleType(object obj)
            {
                if (!IsSimpleType(obj))
                {
                    throw new ArgumentException("value " + obj + "is not belong simple type");
                }
            }

            private static bool IsSimpleType(object obj)
            {
                if (obj == null)
                {
                    return false;
                }

                return obj is string || obj.GetType().IsPrimitive;
            }

            private static string NormalizeString(object value)
            {
                return value is bool ? value.ToString().ToLower() : value.ToString();
            }
        }
    }
}
#endif