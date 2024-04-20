﻿using System;

namespace Godot
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public class OnReadyAttribute : Attribute
    {
        public string NodePath;

        public OnReadyAttribute(string nodePath = "") => NodePath = nodePath;
    }
}