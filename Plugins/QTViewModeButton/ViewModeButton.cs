//    This file is part of QTTabBar, a shell extension for Microsoft
//    Windows Explorer.
//    Copyright (C) 2010  Quizo, Paul Accisano
//
//    QTTabBar is free software: you can redistribute it and/or modify
//    it under the terms of the GNU General Public License as published by
//    the Free Software Foundation, either version 3 of the License, or
//    (at your option) any later version.
//
//    QTTabBar is distributed in the hope that it will be useful,
//    but WITHOUT ANY WARRANTY; without even the implied warranty of
//    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//    GNU General Public License for more details.
//
//    You should have received a copy of the GNU General Public License
//    along with QTTabBar.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using QTPlugin;
using QTPlugin.Interop;

namespace QuizoPlugins {
    //[Plugin(PluginType.Interactive, Author = "Quizo", Name = "Change Folder View Mode", Version = "1.0.0.0", Description = "Button to change folder viewmode for XP.")]
    [Plugin(PluginType.Interactive, Author = "indiff", Name = "查看", Version = "1.0.0.0", Description = "添加按钮改变查看方式.")]
    public class ViewModeButton : IBarCustomItem
    {
        private IPluginServer pluginServer;
        private IShellBrowser shellBrowser;

        private ToolStripButton button;
        private FolderViewModeWindow fvmw;

        private string[] ResStrs;


        #region IPluginClient members

        public void Open(IPluginServer pluginServer, IShellBrowser shellBrowser) {
            this.pluginServer = pluginServer;
            this.shellBrowser = shellBrowser;

            if(!this.pluginServer.TryGetLocalizedStrings(this, 7, out ResStrs)) {
                // if localized strings not found, falls back to default strings in assembly resource of this dll.

                // this plugin has 2 language resources in assemly, but usually there's no need to do like this.
                // it only has to have a resource of your own language.

                if (CultureInfo.CurrentCulture.Parent.Name == "ja")
                    ResStrs = Resource.viewModes_Ja.Split(new char[] { ';' });
                else if (CultureInfo.CurrentCulture.Parent.Name == "zh-CHS")
                    ResStrs = Resource.viewModes_zh.Split(new char[] { ';' });
                else
                    ResStrs = Resource.viewModes.Split(new char[] { ';' });
            }

            this.pluginServer.NavigationComplete += pluginServer_NavigationComplete;
        }

        public void Close(EndCode code) {
            pluginServer = null;
            shellBrowser = null;

            if(button != null) {
                button.Dispose();
            }

            if(fvmw != null) {
                fvmw.Dispose();
            }
        }

        public bool QueryShortcutKeys(out string[] actions) {
            actions = null;
            return false;
        }

        public void OnMenuItemClick(MenuType menuType, string menuText, ITab tab) {
        }

        public bool HasOption {
            get {
                return false;
            }
        }

        public void OnOption() {
        }

        public void OnShortcutKeyPressed(int iIndex) {
        }

        #endregion


        #region IBarCustomItem members

        public ToolStripItem CreateItem(bool fLarge, DisplayStyle displayStyle) {
            if(button == null) {
                button = new ToolStripButton();
                button.Text =
                button.ToolTipText = ResStrs[6];
                button.MouseDown += button_MouseDown;
            }

            switch(displayStyle) {
                case DisplayStyle.NoLabel:
                case DisplayStyle.SelectiveText:
                    button.DisplayStyle = ToolStripItemDisplayStyle.Image;
                    break;
                case DisplayStyle.ShowTextLabel:
                    button.DisplayStyle = ToolStripItemDisplayStyle.ImageAndText;
                    break;
            }

            button.Image = fLarge ? Resource.ViewModeButton_large : Resource.ViewModeButton_small;

            return button;
        }

        #endregion



        private void pluginServer_NavigationComplete(object sender, PluginEventArgs e) {
            UpdateButtonImage(GetCurrentViewMode());
        }

        private void button_MouseDown(object sender, MouseEventArgs e) {
            if(e.Button == MouseButtons.Left) {
                if(fvmw == null) {
                    fvmw = new FolderViewModeWindow(ResStrs);
                    fvmw.ViewModeChanged += fvmw_ViewModeChanged;
                }

                fvmw.ShowWindow(Control.MousePosition, GetCurrentViewMode());
            }
        }

        private void fvmw_ViewModeChanged(object sender, EventArgs e) {
            IShellView shellView = null;
            try {
                if(0 == shellBrowser.QueryActiveShellView(out shellView)) {
                    IFolderView folderView = shellView as IFolderView;

                    if(folderView != null) {
                        FOLDERVIEWMODE currentMode = 0;
                        folderView.GetCurrentViewMode(ref currentMode);

                        FOLDERVIEWMODE mode = fvmw.ViewMode;

                        if(currentMode != mode) {
                            folderView.SetCurrentViewMode(mode);
                            UpdateButtonImage(mode);
                        }
                    }
                }
            }
            catch {
            }
            finally {
                if (shellView != null)
                {
                  //  Marshal.ReleaseComObject(shellView);
                }
                    
            }
        }


        private FOLDERVIEWMODE GetCurrentViewMode() {
            IShellView shellView = null;
            try {
                if(0 == shellBrowser.QueryActiveShellView(out shellView)) {
                    IFolderView folderView = shellView as IFolderView;

                    if(folderView != null) {
                        FOLDERVIEWMODE currentMode = 0;
                        folderView.GetCurrentViewMode(ref currentMode);

                        return currentMode;
                    }
                }
            }
            catch (Exception e){
                pluginServer.MakeErrorLog(e, "ViewModeButton GetCurrentViewMode");
            }
            finally {
                if (shellView != null)
                {
                    // Marshal.ReleaseComObject(shellView);
                }
                    
            }

            return FOLDERVIEWMODE.FVM_ICON;
        }

        /// <summary>
        /// 更新按钮图片
        /// 修复皮肤配置空图片报错
        /// check mode & button is null ?
        /// </summary>
        /// <param name="mode"></param>
        private void UpdateButtonImage(FOLDERVIEWMODE mode) {
            if (mode == null || null == button )
            {
                return;
            }
            switch(mode) {
                case FOLDERVIEWMODE.FVM_THUMBSTRIP:
                    if (null != Resource.imgFilm)
                     button.Image = Resource.imgFilm;
                    break;
                case FOLDERVIEWMODE.FVM_THUMBNAIL:
                    if (null != Resource.imgThumb) 
                        button.Image = Resource.imgThumb;
                    break;
                case FOLDERVIEWMODE.FVM_TILE:
                    if (null != Resource.imgTiles)
                        button.Image = Resource.imgTiles;
                    break;
                case FOLDERVIEWMODE.FVM_ICON:
                    if (null != Resource.imgIcon)
                        button.Image = Resource.imgIcon;
                    break;
                case FOLDERVIEWMODE.FVM_LIST:
                    if (null != Resource.imgList)
                        button.Image = Resource.imgList;
                    break;
                case FOLDERVIEWMODE.FVM_DETAILS:
                    if (null != Resource.imgDetails)
                        button.Image = Resource.imgDetails;
                    break;
            }
        }
    }
}
