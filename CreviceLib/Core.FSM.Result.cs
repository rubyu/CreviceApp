﻿using System;
using System.Collections.Generic;
using System.Text;

namespace Crevice.Core.FSM
{
    public struct Result
    {
        public readonly bool EventIsConsumed;
        public readonly IState NextState;
        public Result(bool eventIsConsumed, IState nextState)
        {
            EventIsConsumed = eventIsConsumed;
            NextState = nextState;
        }
    }
}