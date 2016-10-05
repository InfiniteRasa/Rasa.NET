using System;

namespace Rasa.Cryptography
{
    public class Bignum
    {
        private const int BignumSize = 256; //Wert: 0 - 2^65536 

        public byte[] Content { get; } = new byte[BignumSize];

        public bool Reset()
        {
            Array.Clear(Content, 0, Content.Length);
            return true;
        }

        public bool SetUInt32(uint val)
        {
            Reset();
            Array.Copy(BitConverter.GetBytes(val), Content, 4);
            return true;
        }

        public bool Copy(Bignum src)
        {
            Array.Copy(src.Content, Content, src.Content.Length);
            return true;
        }

        //Addiert zwei Bignums (A = B + C), gibt den Überlauf zurück
        public uint Add(Bignum b, Bignum c)
        {
            var overflow = 0U;

            for (var i = 0; i < BignumSize; ++i)
            {
                //Addiere zu overflow die beiden Bignum Ziffern
                overflow += (uint)b.Content[i] + c.Content[i];

                //Speichere aktuellen Ziffernwert
                Content[i] = (byte)(overflow & 0xFF);

                overflow = overflow >> 8;
            }

            return overflow;
        }

        //Subtrahiert zwei Bignums (Result = B - C), gibt den Überlauf zurück
        public uint Sub(Bignum b, Bignum c)
        {
            var overflow = 1U;

            for (var i = 0; i < BignumSize; i++)
            {
                //Addiere zu overflow die beiden Bignum Ziffern b + ~d
                overflow += b.Content[i] + (uint)((~c.Content[i]) & 0xFF);

                //Speichere aktuellen Ziffernwert
                Content[i] = (byte)(overflow & 0xFF);

                //Rücke overflow Wert
                overflow = overflow >> 8;
            }

            return overflow;
        }

        //Multipliziert zwei Bignums (result = a * b)
        public bool Mul(Bignum a, Bignum b)
        {
            //Zwischenspeicher für Ziffern-Multiplikationsergebniss
            var storer = new Bignum();
            var resultnew = new Bignum();

            var o1 = new Bignum();
            var o2 = new Bignum();

            o1.Copy(a);
            o2.Copy(b);

            //Ziffernwertspeicher
            resultnew.Reset();

            for (var i = 0; i < BignumSize; ++i)
            {
                //Setze storer und overflow auf 0
                storer.Reset();

                var overflow = 0U;

                //Berechne Ziffernmultiplikation
                for (var i2 = 0; i2 < (BignumSize - i); ++i2)
                {
                    //Multipliziere Ziffern
                    overflow += o1.Content[i] * (uint)o2.Content[i2];

                    //Speichere Ziffer
                    storer.Content[i + i2] = (byte)(overflow & 0xFF);

                    //Schiebe Ziffern
                    overflow = overflow >> 8;
                }
                //Addiere Ziffernprodukt zu result
                resultnew.Add(resultnew, storer);
            }

            //Kopiere Ergebniss
            Copy(resultnew);

            //Return erfolg
            return true;
        }

        //Dividiert einen Bignum (result = a / c)
        public bool Div(Bignum a, Bignum b)
        {
            var o1 = new Bignum();
            var o2 = new Bignum();

            Reset();

            o1.Copy(a);
            o2.Copy(b);

            //Fange falsche Werte ab
            if (o1.IsZero() || o2.IsZero())
                return false;

            //Suche maximum
            var shiftCount = 0;
            while (o1.Compare(o2) >= 0 && (o2.Content[BignumSize - 1] & 0x80) == 0)
            {
                ++shiftCount;
                o2.Double();
            }

            //Mache den letzten Verdoppler Rückgängig
            o2.Half();

            //Solange b ist nicht Null
            while (shiftCount > 0)
            {
                //Rücke result
                Double();

                //Passt b in a?
                if (o1.Compare(o2) >= 0) // (a-b)>=0
                {
                    //Subtrahiere von a
                    o1.Sub(o1, o2);

                    //Setze Result
                    Content[0] |= 1;
                }

                //Halbiere b
                o2.Half();

                //Zähle shiftanzahl
                --shiftCount;
            }

            //Return erfolg
            return true;
        }

        //Berechnet den Modulo einer Bignumdivision (result = A%B)
        public bool Mod(Bignum a, Bignum b)
        {
            var factor = new Bignum();

            //Runde Factor auf a/b*b
            factor.Div(a, b);
            factor.Mul(factor, b);

            //Berechne Rest
            Sub(a, factor);

            //Return Erfolg
            return true;
        }

        //Modulare Exponentiation (result = (B^E)%N)
        public bool ModExp(Bignum b, Bignum e, Bignum n)
        {
            var p = new Bignum();
            var exponent = new Bignum();

            var bitCount = e.CountBits();

            //Setze p auf 1
            p.Reset();
            p.SetUInt32(0x01);

            //Kopiere Exponent
            exponent.Copy(e);

            //Beginne schleife
            while (bitCount >= 0)
            {
                p.Mul(p, p);
                p.Mod(p, n);

                //Falls ungerade multiplizierte mit Basis
                if ((exponent.Content[bitCount / 8] & (1 << (bitCount % 8))) != 0)
                {
                    p.Mul(p, b);
                    p.Mod(p, n);
                }

                //Halbiere exponent
                //Bignum_Half(&exponent);
                --bitCount;
            }

            //Kopiere Ergebniss
            Copy(p);

            //Return erfolg
            return true;
        }

        //Halbiert einen Bignum (a = a / 2)
        public bool Half()
        {
            //Shiftbit ist 0
            byte shiftbit = 0;
            for (var i = BignumSize - 1; i >= 0; --i)
            {
                //Errechne neuen Ziffernwert
                var newvalue = (byte)((Content[i] >> 1) | (shiftbit << 7));

                //Merke shiftbit
                shiftbit = (byte)(Content[i] & 0x01);

                //Setze neuen Ziffernwert
                Content[i] = newvalue;
            }

            //Return erfolg
            return true;
        }

        //Verdoppelt einen Bignum (a = a * 2)
        public bool Double()
        {
            //Shiftbit ist 0
            byte shiftbit = 0;

            for (var i = 0; i < BignumSize; ++i)
            {
                //Errechne neuen Ziffernwert
                var newvalue = (byte)((Content[i] << 1) | (shiftbit >> 7));

                //Merke shiftbit
                shiftbit = (byte)(Content[i] & 0x80);

                //Setze neuen Ziffernwert
                Content[i] = newvalue;
            }

            //Return erfolg
            return true;
        }

        //Bignumvergleiche

        //Vergleicht zwei Bignums (-1,0,1)
        public int Compare(Bignum b)
        {
            for (var i = (BignumSize - 1); i >= 0; --i)
            {
                var vergleich = Content[i] - b.Content[i];

                //Falls unterschiedlich, gib Unterschied zurück
                if (vergleich != 0)
                    return vergleich;
            }

            //Return gleich
            return 0;
        }

        //Vergleicht einen Bignum mit 0
        public bool IsZero()
        {
            //Für alle Ziffern
            for (var i = 0; i < BignumSize; ++i)
                if (Content[i] != 0)
                    return false; //Return false falls Ziffer ungleich 0

            return true;
        }

        //Gibt die Stelle des ersten Bits zurück(Bsp: 0x100 wäre 8)
        public int CountBits()
        {
            for (var i = (BignumSize - 1) * 8; i >= 0; --i)
                if ((Content[i / 8] & (1 << (i % 8))) != 0)
                    return i;

            return 0;
        }

        //Liest einen Bignum aus dem Speicher
        public bool Read(byte[] p, int offset, int digitCount)
        {
            Reset();

            for (var i = 0; i < digitCount; ++i)
                Content[i] = p[offset + i];

            //Return Erfolg
            return true;
        }

        //Liest einen Bignum aus dem Speicher im Big Endian Format.
        public bool ReadBigEndian(byte[] p, int offset, int digitCount)
        {
            Reset();

            for (var i = 0; i < digitCount; ++i)
                Content[digitCount - i - 1] = p[offset + i];

            //Return Erfolg
            return true;
        }

        //Schreibt einen Bignum in den Speicher
        public bool WriteTo(byte[] p, int digitCount)
        {
            Array.Clear(p, 0, digitCount);

            for (var i = 0; i < digitCount; ++i)
                p[i] = Content[i];

            //Return Erfolg
            return true;
        }

        //Schreibt einen Bignum in den Speicher im Big Endian Format
        //Achtung: Funktioniert nicht ganz fehlerfrei wenn DigitCount > Eigentliche Anzahl der Stellen
        public bool WriteToBigEndian(byte[] p, int off, int digitCount)
        {
            Array.Clear(p, off, digitCount);

            for (var i = 0; i < digitCount; ++i)
                p[off + i] = Content[digitCount - i - 1];

            //Return Erfolg
            return true;
        }

        //Gibt die Länge eines Bignum in Byte-Stellen zurück
        public int Length()
        {
            for (var i = (BignumSize - 1); i >= 0; --i)
                if (Content[i] != 0)
                    return i + 1;

            return 0;
        }
    }
}
