﻿using System;
using ACE.Entity.Enum.Properties;

namespace ACE.Server.Network.GameMessages.Messages
{
    public class GameMessagePrivateUpdatePropertyBool : GameMessage
    {
        public GameMessagePrivateUpdatePropertyBool(Session session, PropertyBool property, bool value) : base(GameMessageOpcode.PrivateUpdatePropertyBool, GameMessageGroup.UIQueue)
        {
            Writer.Write(session.Player.Sequences.GetNextSequence(Sequence.SequenceType.PrivateUpdatePropertyBool));
            Writer.Write((uint)property);
            Writer.Write(Convert.ToUInt32(value));
        }
    }
}
