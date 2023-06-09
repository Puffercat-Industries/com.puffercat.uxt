﻿namespace Puffercat.Uxt.SimpleECS
{
    public record TagComponent<T> : IComponent where T : TagComponent<T>, new()
    {
        public static readonly T instance = new();
        
        public IComponent Copy()
        {
            return instance;
        }
    }
}