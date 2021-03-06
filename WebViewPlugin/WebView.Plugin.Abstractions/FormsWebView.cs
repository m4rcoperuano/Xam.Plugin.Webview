﻿using System;
using System.Linq;
using System.Collections.Generic;
using Xamarin.Forms;
using Xam.Plugin.Abstractions.Extensions;
using Xam.Plugin.Abstractions.Enumerations;
using Xam.Plugin.Abstractions.Events.Inbound;
using Xam.Plugin.Abstractions.Events.Outbound;
using Xam.Plugin.Abstractions.DTO;
using WebView.Plugin.Abstractions.Events.Inbound;

namespace Xam.Plugin.Abstractions
{
    public class FormsWebView : View
    {

        public static readonly BindableProperty NavigatingProperty = BindableProperty.Create(nameof(Navigating), typeof(bool), typeof(FormsWebView), false);
        public static readonly BindableProperty SourceProperty = BindableProperty.Create(nameof(Source), typeof(string), typeof(FormsWebView), null);
        public static readonly BindableProperty ContentTypeProperty = BindableProperty.Create(nameof(ContentType), typeof(WebViewContentType), typeof(FormsWebView), WebViewContentType.Internet);
        public static readonly BindableProperty RegisteredActionsProperty = BindableProperty.Create(nameof(RegisteredActions), typeof(Dictionary<object, Dictionary<string, Action<string>>>), typeof(FormsWebView), new Dictionary<object, Dictionary<string, Action<string>>>());
        public static readonly BindableProperty BaseUrlProperty = BindableProperty.Create(nameof(BaseUrl), typeof(string), typeof(FormsWebView), null);
        public static readonly BindableProperty CanGoBackProperty = BindableProperty.Create(nameof(CanGoBack), typeof(bool), typeof(FormsWebView), false);
        public static readonly BindableProperty CanGoForwardProperty = BindableProperty.Create(nameof(CanGoForward), typeof(bool), typeof(FormsWebView), false);

        public string BaseUrl
        {
            get { return (string)GetValue(BaseUrlProperty); }
            set { SetValue(BaseUrlProperty, value); }
        }

        public bool Navigating
        {
            get { return (bool) GetValue(NavigatingProperty); }
            set { SetValue(NavigatingProperty, value); }
        }

        public string Source
        {
            get { return (string) GetValue(SourceProperty); }
            set { SetValue(SourceProperty, value); _controlEventAbstraction.Target.PerformNavigation(this, value, ContentType);}
        }

        public bool CanGoBack
        {
            get { return (bool) GetValue(CanGoBackProperty); }
            private set { SetValue(CanGoBackProperty, value); }
        }

        public bool CanGoForward
        {
            get { return (bool) GetValue(CanGoForwardProperty); }
            private set { SetValue(CanGoForwardProperty, value); }
        }

        public WebViewContentType ContentType
        {
            get { return (WebViewContentType) GetValue(ContentTypeProperty); }
            set { SetValue(ContentTypeProperty, value); }
        }

        Dictionary<object, Dictionary<string, Action<string>>> RegisteredActions
        {
            get { return (Dictionary<object, Dictionary<string, Action<string>>>) GetValue(RegisteredActionsProperty); }
            set { SetValue(RegisteredActionsProperty, value); }
        }

        // This is used as the object key for global callbacks, it is not perfectly unique but should be unique enough to not match any existing FWV objects.
        static object _globalKey = -1;

        internal WebViewControlEventAbstraction _controlEventAbstraction;
        public delegate NavigationRequestedDelegate WebViewNavigationStartedEventArgs(NavigationRequestedDelegate eventObj);
        public event WebViewNavigationStartedEventArgs OnNavigationStarted;

        public delegate void WebViewNavigationCompletedEventArgs(NavigationCompletedDelegate eventObj);
        public event WebViewNavigationCompletedEventArgs OnNavigationCompleted;

        public delegate void WebViewNavigationErrorEventArgs(NavigationErrorDelegate eventObj);
        public event WebViewNavigationErrorEventArgs OnNavigationError;

        public delegate void JavascriptResponseEventArgs(JavascriptResponseDelegate eventObj);
        public event JavascriptResponseEventArgs OnJavascriptResponse;

        public delegate void ContentLoadedEventArgs(ContentLoadedDelegate eventObj);
        public event ContentLoadedEventArgs OnContentLoaded;

        public FormsWebView()
        {
            _controlEventAbstraction = new WebViewControlEventAbstraction() { Source = new WebViewControlEventStub() };

            // Register Global
            if (!RegisteredActions.ContainsKey(_globalKey))
                RegisteredActions.Add(_globalKey, new Dictionary<string, Action<string>>());

            // Register Local
            if (!RegisteredActions.ContainsKey(this))
                RegisteredActions.Add(this, new Dictionary<string, Action<string>>());
        }

        public void GoBack()
        {
            if (CanGoBack)
                _controlEventAbstraction.Target.NavigateThroughStack(this, false);
        }

        public void GoForward()
        {
            if (CanGoForward)
                _controlEventAbstraction.Target.NavigateThroughStack(this, true);
        }

        public void InjectJavascript(string js)
        {
            _controlEventAbstraction.Target.InjectJavascript(this, js);
        }

        [Obsolete("This methods name has been updated to better reflect its use case. Please use RegisterGlobalCallback instead.")]
        public void RegisterCallback(string name, Action<string> callback) => RegisterGlobalCallback(name, callback);

        [Obsolete("This methods name has been updated to better reflect its use case. Please use RemoveGlobalCallback instead.")]
        public void RemoveCallback(string name) => RemoveGlobalCallback(name);

        [Obsolete("This methods name has been updated to better reflect its use case. Please use GetGlobalCallbacks instead.")]
        public string[] GetAllCallbacks() => GetGlobalCallbacks();

        [Obsolete("This methods name has been updated to better reflect its use case. Please use RemoveAllGlobalCallbacks instead.")]
        public void RemoveAllCallbacks() => RemoveAllGlobalCallbacks();

        public void RegisterGlobalCallback(string name, Action<string> callback)
        {
            if (!RegisteredActions[_globalKey].ContainsKey(name))
            {
                RegisteredActions[_globalKey].Add(name, callback);
                _controlEventAbstraction.Target.NotifyCallbacksChanged(this, name, true);
            }
        }

        public void RemoveGlobalCallback(string name)
        {
            if (RegisteredActions[_globalKey].ContainsKey(name))
                RegisteredActions[_globalKey].Remove(name);
        }

        public string[] GetGlobalCallbacks()
        {
            return RegisteredActions[_globalKey].Keys.ToArray();
        }

        public void RemoveAllGlobalCallbacks()
        {
            RegisteredActions[_globalKey].Clear();
        }

        public void RegisterLocalCallback(string name, Action<string> callback)
        {
            if (!RegisteredActions[this].ContainsKey(name))
            {
                RegisteredActions[this].Add(name, callback);
                _controlEventAbstraction.Target.NotifyCallbacksChanged(this, name, false);
            }
        }

        public void RemoveLocalCallback(string name)
        {
            if (RegisteredActions[this].ContainsKey(name))
                RegisteredActions[this].Remove(name);
        }

        public string[] GetLocalCallbacks()
        {
            return RegisteredActions[this].Keys.ToArray();
        }

        public void RemoveAllLocalCallbacks()
        {
            RegisteredActions[this].Clear();
        }

        /// <summary>
        /// Internal Use Only.
        /// </summary>
        /// <param name="type">The type of event</param>
        /// <param name="eventObject">The WVE object to pass</param>
        /// <returns></returns>
        public object InvokeEvent(WebViewEventType type, WebViewDelegate eventObject)
        {
            switch (type)
            {
                case WebViewEventType.NavigationRequested:
                    return OnNavigationStarted == null ? eventObject as NavigationRequestedDelegate : OnNavigationStarted.Invoke(eventObject as NavigationRequestedDelegate);

                case WebViewEventType.NavigationComplete:
                    OnNavigationCompleted?.Invoke(eventObject as NavigationCompletedDelegate);
                    break;

                case WebViewEventType.NavigationError:
                    OnNavigationError?.Invoke(eventObject as NavigationErrorDelegate);
                    break;

                case WebViewEventType.ContentLoaded:
                    OnContentLoaded?.Invoke(eventObject as ContentLoadedDelegate);
                    break;

                case WebViewEventType.NavigationStackUpdate:
                    CanGoBack = (eventObject as NavigationStackUpdateDelegate).CanGoBack;
                    CanGoForward = (eventObject as NavigationStackUpdateDelegate).CanGoForward;
                    break;

                case WebViewEventType.JavascriptCallback:
                    var data = (eventObject as JavascriptResponseDelegate).Data;
                    ActionResponse ar;

                    if (data.ValidateJSON() && (ar = data.AttemptParseActionResponse()) != null)
                    {
                        // Attempt Locals
                        if (RegisteredActions[this].ContainsKey(ar.Action))
                            RegisteredActions[this][ar.Action]?.Invoke(ar.Data);

                        // Attempt Globals
                        if (RegisteredActions[_globalKey].ContainsKey(ar.Action))
                            RegisteredActions[_globalKey][ar.Action]?.Invoke(ar.Data);
                    }
                    else
                    {
                        OnJavascriptResponse?.Invoke(eventObject as JavascriptResponseDelegate);
                    }

                    break;
            }
            
            return eventObject;
        }
    }
}
