using System.Collections;
using System.Collections.Generic;

namespace TextCollage.Core.Text
{
    public sealed class CharacterCollection : ICollection<Character>
    {
        #region Fields and Constants

        private readonly List<Character> characters = new List<Character>();

        #endregion

        #region Properties

        public Character this[int index]
        {
            get { return characters[index]; }
        }

        #endregion

        #region  Methods

        public void Add(CharacterCollection collection)
        {
            foreach (Character c in collection)
            {
                Add(c);
            }
        }

        public Character ElementAt(int index)
        {
            return characters[index];
        }

        /// <summary>
        ///     Calculates the total width that is used by the specified interval.
        /// </summary>
        /// <param name="startIndex">Inclusive startindex</param>
        /// <param name="endIndex">Exclusiv endIndex</param>
        /// <returns>The sum of all width of charaters in the given interval.</returns>
        public int TotalWidth(int startIndex, int endIndex)
        {
            int sum = 0;
            for (int i = startIndex; i < endIndex; i++)
            {
                sum += characters[i].Location.Width;
            }
            return sum;
        }

        #endregion

        #region ICollection<Character> Members

        public int Count
        {
            get { return characters.Count; }
        }
        public bool IsReadOnly
        {
            get { return false; }
        }

        public void Add(Character item)
        {
            characters.Add(item);
        }

        public void Clear()
        {
            characters.Clear();
        }

        public bool Contains(Character item)
        {
            return characters.Contains(item);
        }

        public void CopyTo(Character[] array, int arrayIndex)
        {
            characters.CopyTo(array, arrayIndex);
        }

        public bool Remove(Character item)
        {
            return characters.Remove(item);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return characters.GetEnumerator();
        }

        public IEnumerator<Character> GetEnumerator()
        {
            return ((IEnumerable<Character>)characters).GetEnumerator();
        }

        #endregion
    }
}