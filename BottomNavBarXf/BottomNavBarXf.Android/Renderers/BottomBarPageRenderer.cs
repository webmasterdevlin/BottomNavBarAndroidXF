/*
 * BottomNavigationBar for Xamarin Forms
 * Copyright (c) 2016 Thrive GmbH and others (http://github.com/thrive-now).
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 * http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using Android.Views;
using Android.Widget;
using BottomBar.Droid.Renderers;
using BottomBar.XamarinForms;
using BottomNavBarXf.Droid.Utils;
using BottomNavigationBar;
using BottomNavigationBar.Listeners;
using Xamarin.Forms;
using Xamarin.Forms.Platform.Android;
using Xamarin.Forms.Platform.Android.AppCompat;
using IPageController = BottomNavBarXf.Droid.Utils.IPageController;

[assembly: ExportRenderer(typeof(BottomBarPage), typeof(BottomBarPageRenderer))]

namespace BottomNavBarXf.Droid.Renderers
{
    public class BottomBarPageRenderer : VisualElementRenderer<BottomBarPage>, IOnTabClickListener
    {
        private bool _disposed;
        private BottomNavigationBar.BottomBar _bottomBar;
        private FrameLayout _frameLayout;
        private IPageController _pageController;
        private IDictionary<Page, BottomBarBadge> _badges;

        public BottomBarPageRenderer()
        {
            AutoPackage = false;
        }

        #region IOnTabClickListener

        public virtual void OnTabSelected(int position)
        {
            SwitchContent(Element.Children[position]);
            var bottomBarPage = Element as BottomBarPage;
            bottomBarPage.CurrentPage = Element.Children[position];
        }

        public virtual void OnTabReSelected(int position)
        {
        }

        #endregion IOnTabClickListener

        protected override void Dispose(bool disposing)
        {
            if (disposing && !_disposed)
            {
                _disposed = true;

                RemoveAllViews();

                foreach (Page pageToRemove in Element.Children)
                {
                    IVisualElementRenderer pageRenderer = Platform.GetRenderer(pageToRemove);

                    if (pageRenderer != null)
                    {
                        pageRenderer.ViewGroup.RemoveFromParent();
                        pageRenderer.Dispose();
                    }

                }

                if (_badges != null)
                {
                    _badges.Clear();
                    _badges = null;
                }

                if (_bottomBar != null)
                {
                    _bottomBar.SetOnTabClickListener(null);
                    _bottomBar.Dispose();
                    _bottomBar = null;
                }

                if (_frameLayout != null)
                {
                    _frameLayout.Dispose();
                    _frameLayout = null;
                }
            }
            base.Dispose(disposing);
        }

        protected override void OnAttachedToWindow()
        {
            base.OnAttachedToWindow();
            _pageController.SendAppearing();
        }

        protected override void OnDetachedFromWindow()
        {
            base.OnDetachedFromWindow();
            _pageController.SendDisappearing();
        }

        protected override void OnElementChanged(ElementChangedEventArgs<BottomBarPage> e)
        {
            base.OnElementChanged(e);

            if (e.NewElement != null)
            {
                BottomBarPage bottomBarPage = e.NewElement;

                if (_bottomBar == null)
                {
                    _pageController = PageController.Create(bottomBarPage);
                    _frameLayout = new FrameLayout(Forms.Context);
                    _frameLayout.LayoutParameters = new FrameLayout.LayoutParams(LayoutParams.MatchParent, LayoutParams.MatchParent, GravityFlags.Fill);
                    AddView(_frameLayout, 0);
                    _bottomBar = BottomNavigationBar.BottomBar.Attach(_frameLayout, null);
                    _bottomBar.NoTabletGoodness();
                    if (bottomBarPage.FixedMode)
                    {
                        _bottomBar.UseFixedMode();
                    }

                    switch (bottomBarPage.BarTheme)
                    {
                        case BottomBarPage.BarThemeTypes.Light:
                            break;

                        case BottomBarPage.BarThemeTypes.DarkWithAlpha:
                            _bottomBar.UseDarkThemeWithAlpha(true);
                            break;

                        case BottomBarPage.BarThemeTypes.DarkWithoutAlpha:
                            _bottomBar.UseDarkThemeWithAlpha(false);
                            break;

                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                    _bottomBar.LayoutParameters = new LayoutParams(LayoutParams.MatchParent, LayoutParams.MatchParent);
                    _bottomBar.SetOnTabClickListener(this);

                    UpdateTabs();
                    UpdateBarBackgroundColor();
                    UpdateBarTextColor();
                }

                if (bottomBarPage.CurrentPage != null)
                {
                    SwitchContent(bottomBarPage.CurrentPage);
                }
            }
        }

        protected override void OnElementPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            base.OnElementPropertyChanged(sender, e);

            if (e.PropertyName == nameof(TabbedPage.CurrentPage))
            {
                SwitchContent(Element.CurrentPage);
                UpdateSelectedTabIndex(Element.CurrentPage);
            }
            else if (e.PropertyName == NavigationPage.BarBackgroundColorProperty.PropertyName)
            {
                UpdateBarBackgroundColor();
            }
            else if (e.PropertyName == NavigationPage.BarTextColorProperty.PropertyName)
            {
                UpdateBarTextColor();
            }
        }

        protected virtual void SwitchContent(Page view)
        {
            Context.HideKeyboard(this);

            _frameLayout.RemoveAllViews();

            if (view == null)
            {
                return;
            }

            if (Platform.GetRenderer(view) == null)
            {
                Platform.SetRenderer(view, Platform.CreateRenderer(view));
            }

            _frameLayout.AddView(Platform.GetRenderer(view).ViewGroup);
        }

        protected override void OnLayout(bool changed, int l, int t, int r, int b)
        {
            var width = r - l;
            var height = b - t;
            var context = Context;

            _bottomBar.Measure(MeasureSpecFactory.MakeMeasureSpec(width, MeasureSpecMode.Exactly), MeasureSpecFactory.MakeMeasureSpec(height, MeasureSpecMode.AtMost));
            int tabsHeight = Math.Min(height, Math.Max(_bottomBar.MeasuredHeight, _bottomBar.MinimumHeight));

            if (width > 0 && height > 0)
            {
                _pageController.ContainerArea = new Rectangle(0, 0, context.FromPixels(width), context.FromPixels(_frameLayout.MeasuredHeight));
                ObservableCollection<Element> internalChildren = _pageController.InternalChildren;

                foreach (Element t1 in internalChildren)
                {
                    if (!(t1 is VisualElement child))
                    {
                        continue;
                    }

                    IVisualElementRenderer renderer = Platform.GetRenderer(child);
                    if (renderer is NavigationPageRenderer navigationRenderer)
                    {
                        // TODO:
                    }
                }

                _bottomBar.Measure(MeasureSpecFactory.MakeMeasureSpec(width, MeasureSpecMode.Exactly), MeasureSpecFactory.MakeMeasureSpec(tabsHeight, MeasureSpecMode.Exactly));
                _bottomBar.Layout(0, 0, width, tabsHeight);
            }

            base.OnLayout(changed, l, t, r, b);
        }

        private void UpdateSelectedTabIndex(Page page)
        {
            var index = Element.Children.IndexOf(page);
            _bottomBar.SelectTabAtPosition(index, true);
        }

        private void UpdateBarBackgroundColor()
        {
            if (_disposed || _bottomBar == null)
            {
                return;
            }

            _bottomBar.SetBackgroundColor(Element.BarBackgroundColor.ToAndroid());
        }

        private void UpdateBarTextColor()
        {
            if (_disposed || _bottomBar == null)
            {
                return;
            }
            _bottomBar.SetActiveTabColor(Element.BarTextColor.ToAndroid());
        }

        private void UpdateTabs()
        {
            SetTabItems();
            SetTabColors();
        }

        private void SetTabItems()
        {
            BottomBarTab[] tabs = Element.Children.Select(page =>
            {
                var tabIconId = ResourceManagerEx.IdFromTitle(page.Icon, ResourceManager.DrawableClass);
                return new BottomBarTab(tabIconId, page.Title);
            }).ToArray();

            if (tabs.Length > 0)
            {
                _bottomBar.SetItems(tabs);
            }
        }

        private void SetTabColors()
        {
            for (int i = 0; i < Element.Children.Count; ++i)
            {
                Page page = Element.Children[i];

                Color tabColor = BottomBarPageExtensions.GetTabColor(page);

                if (tabColor != Color.Transparent)
                {
                    _bottomBar.MapColorForTab(i, tabColor.ToAndroid());
                }
            }
        }
    }
}