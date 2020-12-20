using System;

namespace Rasa.Structures
{
    using Char;
    using Memory;

    public class LoginData : IPythonDataStruct
    {
        public uint NumLogins { get; set; }
        public DateTime? LastLogin { get; set; }
        public uint TotalTimePlayed { get; set; }

        public LoginData(CharacterEntry entry)
        {
            NumLogins = entry.NumLogins;
            LastLogin = entry.LastLogin;
            TotalTimePlayed = entry.TotalTimePlayed;
        }

        public void Read(PythonReader pr)
        {
            pr.ReadTuple();
            NumLogins = pr.ReadUInt();
            TotalTimePlayed = pr.ReadUInt();
            pr.ReadUInt();
        }

        public void Write(PythonWriter pw)
        {
            pw.WriteTuple(3);
            pw.WriteUInt(NumLogins);
            pw.WriteUInt(TotalTimePlayed);

            if (LastLogin.HasValue && LastLogin.Value < DateTime.UtcNow)
                pw.WriteUInt((uint) Math.Floor((DateTime.UtcNow - LastLogin.Value).TotalMinutes));
            else
                pw.WriteUInt(0);
        }
    }
}
