#if UNITY_EDITOR
namespace Pico.AvatarAssetPreview
{
    public interface IPavPanelExtra
    {
        public bool CheckNavShowWarningWhenSelfIsShow();
        public void OnRefresh();
        public bool IsRefreshVisible();
    }
}
#endif