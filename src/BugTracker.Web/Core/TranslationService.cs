﻿//------------------------------------------------------------------------------
// <autogenerated>
//     This code was generated by a tool.
//     Runtime Version: 1.1.4322.2032
//
//     Changes to this file may cause incorrect behavior and will be lost if 
//     the code is regenerated.
// </autogenerated>
//------------------------------------------------------------------------------

// 
// This source code was auto-generated by wsdl, Version=1.1.4322.2032.
// 

namespace BugTracker.Web.Core
{
    using System;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Web.Services;
    using System.Web.Services.Description;
    using System.Web.Services.Protocols;
    using System.Xml.Serialization;

    /// <remarks />
    [DebuggerStepThrough]
    [DesignerCategory("code")]
    [WebServiceBinding(Name = "TranslationServiceSoap", Namespace = "http://zeta-software.de/TranslationWebService")]
    public class TranslationService : SoapHttpClientProtocol
    {
        /// <remarks />
        public TranslationService()
        {
            Url = "http://www.zeta-software.de/Translator/TranslationService.asmx";
        }

        /// <remarks />
        [SoapDocumentMethod("http://zeta-software.de/TranslationWebService/GetAllTranslationModes",
            RequestNamespace = "http://zeta-software.de/TranslationWebService",
            ResponseNamespace = "http://zeta-software.de/TranslationWebService", Use = SoapBindingUse.Literal,
            ParameterStyle = SoapParameterStyle.Wrapped)]
        [return: XmlArrayItem(IsNullable = false)]
        public TranslationMode[] GetAllTranslationModes()
        {
            var results = Invoke("GetAllTranslationModes", new object[0]);
            return (TranslationMode[]) results[0];
        }

        /// <remarks />
        public IAsyncResult BeginGetAllTranslationModes(AsyncCallback callback, object asyncState)
        {
            return BeginInvoke("GetAllTranslationModes", new object[0], callback, asyncState);
        }

        /// <remarks />
        public TranslationMode[] EndGetAllTranslationModes(IAsyncResult asyncResult)
        {
            var results = EndInvoke(asyncResult);
            return (TranslationMode[]) results[0];
        }

        /// <remarks />
        [SoapDocumentMethod("http://zeta-software.de/TranslationWebService/GetTranslationModeByObjectID",
            RequestNamespace = "http://zeta-software.de/TranslationWebService",
            ResponseNamespace = "http://zeta-software.de/TranslationWebService", Use = SoapBindingUse.Literal,
            ParameterStyle = SoapParameterStyle.Wrapped)]
        public TranslationMode GetTranslationModeByObjectID(string objectId)
        {
            var results = Invoke("GetTranslationModeByObjectID", new object[]
            {
                objectId
            });
            return (TranslationMode) results[0];
        }

        /// <remarks />
        public IAsyncResult BeginGetTranslationModeByObjectID(string objectId, AsyncCallback callback,
            object asyncState)
        {
            return BeginInvoke("GetTranslationModeByObjectID", new object[]
            {
                objectId
            }, callback, asyncState);
        }

        /// <remarks />
        public TranslationMode EndGetTranslationModeByObjectID(IAsyncResult asyncResult)
        {
            var results = EndInvoke(asyncResult);
            return (TranslationMode) results[0];
        }

        /// <remarks />
        [SoapDocumentMethod("http://zeta-software.de/TranslationWebService/Translate",
            RequestNamespace = "http://zeta-software.de/TranslationWebService",
            ResponseNamespace = "http://zeta-software.de/TranslationWebService", Use = SoapBindingUse.Literal,
            ParameterStyle = SoapParameterStyle.Wrapped)]
        public string Translate(TranslationMode translationMode, string textToTranslate)
        {
            var results = Invoke("Translate", new object[]
            {
                translationMode,
                textToTranslate
            });
            return (string) results[0];
        }

        /// <remarks />
        public IAsyncResult BeginTranslate(TranslationMode translationMode, string textToTranslate,
            AsyncCallback callback,
            object asyncState)
        {
            return BeginInvoke("Translate", new object[]
            {
                translationMode,
                textToTranslate
            }, callback, asyncState);
        }

        /// <remarks />
        public string EndTranslate(IAsyncResult asyncResult)
        {
            var results = EndInvoke(asyncResult);
            return (string) results[0];
        }
    }

    /// <remarks />
    [XmlType(Namespace = "http://zeta-software.de/TranslationWebService")]
    public class TranslationMode
    {
        /// <remarks />
        public string ObjectID;

        /// <remarks />
        public string VisualNameDE;

        /// <remarks />
        public string VisualNameEN;
    }
}