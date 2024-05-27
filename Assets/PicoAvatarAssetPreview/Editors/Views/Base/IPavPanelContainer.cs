#if UNITY_EDITOR
namespace Pico
{
    namespace AvatarAssetPreview
    {
        /**
         * 
         */
        public interface IPavPanelContainer
        {
#region For Single panel mode.

            /**
             * @brief Show with single Panel.
             * @param asNextPanel whether show as next panel and can go back from previous panel.
             */
            void ShowPanel(PavPanel panel, bool asNextPanel, UnityEngine.Object dataObj);

#endregion

#region For Panel Table.

            /**
             * Adds a panel for Panel Table.
             */
            void AddPanel(PavPanel panel);

            /**
             * Remove a panel from Panel Table.
             */
            void RemovePanel(PavPanel panel);

#endregion
        }
    }
}
#endif