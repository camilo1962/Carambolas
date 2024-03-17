using System;
using System.Text;

namespace CaromBilliards
{
    public static class Utils
    {
        public static string GetScoreString(int _score)
        {
            StringBuilder sb = new StringBuilder("Carambolas : ");
            sb.Append(_score);
            return sb.ToString();
        }

        public static string GetElapsedTimeString(float _time, bool _shortVersion = false)
        {
            StringBuilder sb = new StringBuilder(25);
            if (_shortVersion)
                sb.Append("Tiempo : ");
            else
                sb.Append("Tiempo transcurrido : ");

            
            sb.Append(((int)(_time / 60)).ToString());
            int sec = ((int)_time) % 60;
            if (sec >= 10)
            {
                
                sb.Append(":");
                sb.Append(sec.ToString());
            }
            else
            {
                sb.Append(((int)(_time / 60)).ToString());
                sb.Append(":0");
                sb.Append(sec.ToString());
            }        

            return sb.ToString();
        }

        public static string GetShotsNumberString(int _nb)
        {
            StringBuilder sb = new StringBuilder("Tiradas : ");
            sb.Append(_nb);
            return sb.ToString();
        }
    }
}