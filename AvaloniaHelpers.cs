using System.Collections;
using System.Collections.Generic;

namespace ClassJsonEditor
{
    public static class AvaloniaHelpers
    {
        public static T First<T>(this IList list) where T : class
        {
            if (list != null)
            {
                if (list.Count > 0)
                {
                    if (list[0] is T ret)
                    {
                        return ret;
                    }
                }
            }

            return null;
        }
    }
}