﻿/*
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

using Android.Views;

namespace BottomNavBarXf.Droid.Utils
{
    internal static class MeasureSpecFactory
    {
        public static int GetSize(int measureSpec)
        {
            const int modeMask = 0x3 << 30;
            return measureSpec & ~modeMask;
        }

        public static int MakeMeasureSpec(int size, MeasureSpecMode mode) => size + (int)mode;
        
    }
}