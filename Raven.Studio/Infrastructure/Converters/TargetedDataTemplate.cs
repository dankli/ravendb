﻿using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace Raven.Studio.Infrastructure.Converters
{
    public class TargetedDataTemplate
    {
        public string TargetType { get; set; }

        public DataTemplate Template { get; set; }
    }
}
