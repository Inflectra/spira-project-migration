﻿#pragma checksum "..\..\..\..\Pages\Transfer\trnsProgressOut.xaml" "{406ea660-64cf-4c82-b6f0-42d48172a799}" "A067D641325B3ADD5B408DC4586901A4"
//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Media.TextFormatting;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Shell;


namespace Inflectra.SpiraTest.Utilities.ProjectMigration {
    
    
    /// <summary>
    /// trnsProgressOut
    /// </summary>
    public partial class trnsProgressOut : System.Windows.Controls.UserControl, System.Windows.Markup.IComponentConnector {
        
        
        #line 22 "..\..\..\..\Pages\Transfer\trnsProgressOut.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Documents.Run txtProjectName;
        
        #line default
        #line hidden
        
        
        #line 25 "..\..\..\..\Pages\Transfer\trnsProgressOut.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.TextBlock txtAction;
        
        #line default
        #line hidden
        
        
        #line 26 "..\..\..\..\Pages\Transfer\trnsProgressOut.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Canvas pnlBar;
        
        #line default
        #line hidden
        
        
        #line 27 "..\..\..\..\Pages\Transfer\trnsProgressOut.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.ProgressBar barProgress;
        
        #line default
        #line hidden
        
        
        #line 28 "..\..\..\..\Pages\Transfer\trnsProgressOut.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.TextBlock txtPercentage;
        
        #line default
        #line hidden
        
        
        #line 30 "..\..\..\..\Pages\Transfer\trnsProgressOut.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.TextBlock txtError;
        
        #line default
        #line hidden
        
        
        #line 31 "..\..\..\..\Pages\Transfer\trnsProgressOut.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Documents.Run txtErrorMessage;
        
        #line default
        #line hidden
        
        private bool _contentLoaded;
        
        /// <summary>
        /// InitializeComponent
        /// </summary>
        [System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [System.CodeDom.Compiler.GeneratedCodeAttribute("PresentationBuildTasks", "4.0.0.0")]
        public void InitializeComponent() {
            if (_contentLoaded) {
                return;
            }
            _contentLoaded = true;
            System.Uri resourceLocater = new System.Uri("/ProjectMigration;component/pages/transfer/trnsprogressout.xaml", System.UriKind.Relative);
            
            #line 1 "..\..\..\..\Pages\Transfer\trnsProgressOut.xaml"
            System.Windows.Application.LoadComponent(this, resourceLocater);
            
            #line default
            #line hidden
        }
        
        [System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [System.CodeDom.Compiler.GeneratedCodeAttribute("PresentationBuildTasks", "4.0.0.0")]
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Design", "CA1033:InterfaceMethodsShouldBeCallableByChildTypes")]
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")]
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1800:DoNotCastUnnecessarily")]
        void System.Windows.Markup.IComponentConnector.Connect(int connectionId, object target) {
            switch (connectionId)
            {
            case 1:
            this.txtProjectName = ((System.Windows.Documents.Run)(target));
            return;
            case 2:
            this.txtAction = ((System.Windows.Controls.TextBlock)(target));
            return;
            case 3:
            this.pnlBar = ((System.Windows.Controls.Canvas)(target));
            return;
            case 4:
            this.barProgress = ((System.Windows.Controls.ProgressBar)(target));
            
            #line 27 "..\..\..\..\Pages\Transfer\trnsProgressOut.xaml"
            this.barProgress.ValueChanged += new System.Windows.RoutedPropertyChangedEventHandler<double>(this.barProgress_ValueChanged);
            
            #line default
            #line hidden
            return;
            case 5:
            this.txtPercentage = ((System.Windows.Controls.TextBlock)(target));
            return;
            case 6:
            this.txtError = ((System.Windows.Controls.TextBlock)(target));
            return;
            case 7:
            this.txtErrorMessage = ((System.Windows.Documents.Run)(target));
            return;
            }
            this._contentLoaded = true;
        }
    }
}

