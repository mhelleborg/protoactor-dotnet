﻿// -----------------------------------------------------------------------
//   <copyright file="Actor.cs" company="Asynkron AB">
//       Copyright (C) 2015-2020 Asynkron AB All rights reserved
//   </copyright>
// -----------------------------------------------------------------------

using System.Threading.Tasks;

namespace Proto
{
    public static class Actor
    {
        public static readonly Task Done = Task.FromResult(0);
    }
}