using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace LeaderboardExample
{
	public class LeaderboardItem : MonoBehaviour
	{
		public int _Id = -1;
		public Button _Delete = null;
		public Text _Name = null;
		public Text _Score = null;

		public void Start()
		{
			if (_Id == -1)
			{
				_Delete.gameObject.SetActive (false);
			}
		}

		// call from button click
		public void DeleteData()
		{
			LeaderboardManager._Instance.DeleteData(_Id);
		}
	}
}
