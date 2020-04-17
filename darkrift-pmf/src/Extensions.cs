using DarkRift.PMF.Exceptions;
using System;
using System.Collections.Generic;
using System.Text;

namespace DarkRift.PMF
{
    public static class Extensions
    {
        /// <summary>
        /// Nice hack to reuse this bit of code very effectively, this method is just used in List<T> where T is Package, otherwise this method doesn't even show up
        /// </summary>
        /// <param name="list"></param>
        /// <param name="id"></param>
        public static void Remove(this List<Package> list, string id)
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].ID == id)
                {
                    list.RemoveAt(i);
                    break;
                }
            }
        }


        /// <summary>
        /// Same as Remove, but it Retrieves
        /// </summary>
        /// <param name="list"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        public static Package GetPackage(this List<Package> list, string id)
        {
            if (id == null || id.Length == 0)
                throw new ArgumentNullException();

            Package package = list.Find((p) => p.ID == id);
            if (package == null)
                throw new NotFoundException();

            return package;
        }
    }
}
