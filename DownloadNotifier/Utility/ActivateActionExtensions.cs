using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using Microsoft.Toolkit.Uwp.Notifications;
using Windows.UI.Notifications;

namespace DownloadNotifier
{
    public static class ActivateActionExtensions
    {

        /// <summary>Adds a main action that is invoked when toast itself is clicked.</summary>
        public static ToastContentBuilder AddAction(this ToastContentBuilder builder, App.ActivateAction action, string file)
        {
            builder.AddToastActivationInfo(action.ToString() + ":" + file, ToastActivationType.Background);
            return builder;
        }

        /// <summary>
        /// <para>Adds a button for invoking a specific <see cref="App.ActivateAction"/> on a file.</para> 
        /// <para>Gets button content from <see cref="GetActionString(App.ActivateAction)"/>.</para>
        /// </summary>
        public static ToastContentBuilder AddButton(this ToastContentBuilder builder, App.ActivateAction action, string file)
        {
            builder.AddButton(GetActionString(action), ToastActivationType.Background, action.ToString() + ":" + file);
            return builder;
        }

        /// <summary>Sends the toast.</summary>
        public static void Send(this ToastContentBuilder builder) =>
            ToastNotificationManagerCompat.CreateToastNotifier().
                Show(new ToastNotification(builder.GetToastContent().GetXml()));

        /// <summary>Gets that is used on toast button invoke to invoke an action on a file.</summary>
        public static string GetActionString(this App.ActivateAction action) =>
            typeof(App.ActivateAction).
            GetMember(action.ToString()).
            First().
            GetCustomAttribute<DisplayAttribute>()?.Name;

    }

}
