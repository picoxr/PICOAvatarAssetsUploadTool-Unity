#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;

namespace Pico.AvatarAssetBuilder
{
    /// <summary>
    /// Only use network at Unity Editor 
    /// </summary>
    internal static class EditorWebRequest
    {
        /// <summary>
        ///  Network request entry core
        /// </summary>
        /// <param name="request"></param>
        /// <param name="successAction"></param>
        /// <param name="failedAction"></param>
        /// <exception cref="ArgumentException"></exception>
        public static void Send(CommonHttpRequest request, Action<string> successAction,
            Action<NetworkFailure> failedAction = null)
        {
            //build public params list
            var url = GetCommonUrl(request);

            UnityWebRequest call;
            if (request.httpMethod == HttpMethod.Get)
            {
                call = UnityWebRequest.Get(url);
            }
            else if (request.httpMethod == HttpMethod.Post)
            {
                call = BuildPostRequestByType(url, request);
            }
            else
            {
                throw new ArgumentException("unsupported http method " + request.httpMethod);
            }

            // public header params
            foreach (var valuePair in CommonPublicHttpData.AddQueryHeaders().Where(valuePair =>
                         !string.IsNullOrEmpty(valuePair.Value)))
            {
                call.SetRequestHeader(valuePair.Key, valuePair.Value);
            }

            if (request.headers != null)
            {
                foreach (var valuePair in request.headers.Where(valuePair => !string.IsNullOrEmpty(valuePair.Value)))
                {
                    call.SetRequestHeader(valuePair.Key, valuePair.Value);
                }
            }
            
            request.CurrRequest = call;

            call.SendWebRequest().completed += operation =>
            {
                switch (call.result)
                {
                    case UnityWebRequest.Result.Success:
                        var response = JsonConvert.DeserializeObject<CommonResponse>(call.downloadHandler.text);
                        if (response!.Code == 0)
                        {
                            successAction?.Invoke(call.downloadHandler.text);
                        }
                        else
                        {
                            if (response.Code == 10009 || response.Code == 10007)
                            {// error = unauthorized or token out of date, code = 10007
                                CommonDialogWindow.ShowPopupConfirmDialog(null, null,
                                    "Unauthorized or token out of date, Please check!");
                                LoginUtils.Logout();
                                MainWindow.SwitchWindow();
                            }
                            else
                            {
                                var failure = new NetworkFailure(response);
                                failedAction?.Invoke(failure);
                            }
                        }

                        break;
                    case UnityWebRequest.Result.ConnectionError:
                        failedAction?.Invoke(new NetworkFailure(NetworkErrorType.Connection, -1, call.error));
                        break;
                    case UnityWebRequest.Result.ProtocolError:
                        failedAction?.Invoke(new NetworkFailure(NetworkErrorType.Protocol, (int)call.responseCode,
                            call.error));
                        break;
                }
                call.Dispose();
            };
        }

        /// <summary>
        ///  Network request entry core
        /// </summary>
        /// <param name="request"></param>
        /// <param name="progress"></param>
        /// <param name="successAction"></param>
        /// <param name="failedAction"></param>
        /// <exception cref="ArgumentException"></exception>
        public static void Send(CommonHttpRequest request, Action<float> progress, Action<string> successAction,
            Action<NetworkFailure> failedAction = null)
        {
            //build public params list
            var url = GetCommonUrl(request);

            UnityWebRequest call;
            if (request.httpMethod == HttpMethod.Get)
            {
                call = UnityWebRequest.Get(url);
            }
            else if (request.httpMethod == HttpMethod.Post)
            {
                call = BuildPostRequestByType(url, request);
            }
            else
            {
                throw new ArgumentException("unsupported http method " + request.httpMethod);
            }
           
            // public header params
            foreach (var valuePair in CommonPublicHttpData.AddQueryHeaders().Where(valuePair =>
                         !string.IsNullOrEmpty(valuePair.Value)))
            {
                call.SetRequestHeader(valuePair.Key, valuePair.Value);
            }

            if (request.headers != null)
            {
                foreach (var valuePair in request.headers.Where(valuePair => !string.IsNullOrEmpty(valuePair.Value)))
                {
                    call.SetRequestHeader(valuePair.Key, valuePair.Value);
                }
            }
            request.CurrRequest = call;

            call.SendWebRequest().completed += operation =>
            {
                switch (call.result)
                {
                    case UnityWebRequest.Result.Success:
                        var response = JsonConvert.DeserializeObject<CommonResponse>(call.downloadHandler.text);
                        if (response!.Code == 0)
                        {
                            successAction?.Invoke(call.downloadHandler.text);
                        }
                        else
                        {
                            if (response.Code == 10007 || response.Code == 10009)
                            {// error = unauthorized or token out of date, code = 10007
                                CommonDialogWindow.ShowPopupConfirmDialog(null, null,
                                    "Unauthorized or token out of date, Please check!");
                                LoginUtils.Logout();
                                MainWindow.SwitchWindow();
                            }
                            else
                            {
                                var failure = new NetworkFailure(response);
                                failedAction?.Invoke(failure);
                            }
                        }

                        break;
                    case UnityWebRequest.Result.ConnectionError:
                        failedAction?.Invoke(new NetworkFailure(NetworkErrorType.Connection, -1, call.error));
                        break;
                    case UnityWebRequest.Result.ProtocolError:
                        failedAction?.Invoke(new NetworkFailure(NetworkErrorType.Protocol, (int)call.responseCode,
                            call.error));
                        break;
                }
                //call.Dispose();
            };
        }
         
        /// <summary>
        /// Build post request Content-Type by request.contentType
        /// </summary>
        /// <param name="url"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        private static UnityWebRequest BuildPostRequestByType(string url, CommonHttpRequest request)
        {
            Debug.Log("BuildPostRequestByType(): " + url + ", " + request.postDataJson + ", " + request.filePath +
                      ", " + request.contentType);

            UnityWebRequest call = new UnityWebRequest(url, "Post");
            call.downloadHandler = new DownloadHandlerBuffer();

            //build content-type
            string contentTypeString = "application/json; charset=utf-8";
            switch (request.contentType)
            {
                case ContentType.Json:
                    call.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(request.postDataJson));
                    break;
                case ContentType.Multipart:
                    byte[] fileBytes = File.ReadAllBytes(request.filePath);
                    byte[] body = GetMultipartBody(fileBytes, request.filePath, out contentTypeString);

                    // Make my request object and add the raw body. Set anything else you need here
                    call.uploadHandler = new UploadHandlerRaw(body);
                    break;
            }

            call.uploadHandler.contentType = contentTypeString;
            call.SetRequestHeader("Content-Type", contentTypeString);
            return call;
        }

        /// <summary>
        /// Get multipart body
        /// </summary>
        /// <param name="bodyRaw"></param>
        /// <param name="fileName"></param>
        /// <param name="contentTypeString"></param>
        /// <returns></returns>
        private static byte[] GetMultipartBody(byte[] bodyRaw, string fileName, out string contentTypeString)
        {
            // generate a boundary then convert the form to byte[]
            byte[] boundary = UnityWebRequest.GenerateBoundary();
            contentTypeString = String.Concat("multipart/form-data; boundary=", Encoding.UTF8.GetString(boundary));
            Debug.Log("contentTypeString=" + contentTypeString);

            // read a file and add it to the form
            List<IMultipartFormSection> form = new List<IMultipartFormSection>
            {
                new MultipartFormFileSection("file", bodyRaw, fileName, contentTypeString)
            };

            byte[] formSections = UnityWebRequest.SerializeFormSections(form, boundary);
            Debug.Log("formSections=" + Encoding.UTF8.GetString(formSections));

            // // my termination string consisting of CRLF--{boundary}--
            // byte[] terminate = Encoding.UTF8.GetBytes(String.Concat("\r\n--", Encoding.UTF8.GetString(boundary), "--"));

            // Make my complete body from the two byte arrays
            // byte[] body = new byte[formSections.Length + terminate.Length];
            byte[] body = new byte[formSections.Length];
            Buffer.BlockCopy(formSections, 0, body, 0, formSections.Length);
            // Buffer.BlockCopy(terminate, 0, body, formSections.Length, terminate.Length);
            Debug.Log("req body=" + Encoding.UTF8.GetString(body));

            return body;
        }

        /// <summary>
        /// Start download file
        /// </summary>
        /// <param name="request"></param>
        /// <param name="fileName"></param>
        /// <param name="successAction"></param>
        /// <param name="failedAction"></param>
        public static void StartDownload(CommonHttpRequest request, string fileName,
            Action<string> successAction, Action<string, float> progressAction, Action<string, string> failedAction = null)
        {
            using (UnityWebRequest downloader = UnityWebRequest.Get(request.url))
            {
                downloader.downloadHandler = new DownloadHandlerFile(fileName);
                downloader.SendWebRequest();
                
                //update progress
                //var progress = 0 + "%";
                float progress = 0;
                while (!downloader.isDone)
                {
                    progress = downloader.downloadProgress;//(downloader.downloadProgress * 100).ToString("F2") + "%";
                    progressAction?.Invoke(request.url, progress);
                }

                if (downloader.error != null)
                {
                    failedAction?.Invoke(request.url, downloader.error);
                }
                else
                {
                    successAction?.Invoke(request.url);
                }
            }
        }

        /// <summary>
        /// Get common params url
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        private static string GetCommonUrl(CommonHttpRequest request)
        {
            IEnumerable<KeyValuePair<string, string>> finalQuery = null;
            if (request.queryParams != null)
            {
                finalQuery = request.queryParams.Concat(
                    CommonPublicHttpData.AddQueryParams().Where(x => !request.queryParams.Keys.Contains(x.Key)));
            }
            else
            {
                finalQuery = CommonPublicHttpData.AddQueryParams();
            }

            var queryString = string.Empty;
            foreach (var keyValuePair in finalQuery)
            {
                if (keyValuePair.Value.Length <= 0) continue;

                if (queryString.Length > 0)
                    queryString += "&";

                queryString += keyValuePair.Key + "=" + UnityWebRequest.EscapeURL(keyValuePair.Value);
            }

            return request.host + request.path + "?" + queryString;
        }
    }
}
#endif