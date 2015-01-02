using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace LeaderboardExample
{
	public class LeaderboardManager : MonoBehaviour
	{
		// Reference to the prefab item
		public LeaderboardItem _PrefabLeaderboardItem = null;

		// Reference to the row labels
		public LeaderboardItem _LeaderboardLabels = null;

		// Maintain list of leaderboard items
		private List<LeaderboardItem> _leaderboardItems = new List<LeaderboardItem>();

		// cloud 9 username
		public string _C9Username = "tgraupmann";

		// cloud 9 project name
		public string _C9ProjectName = "sailsdemo";

		// call from button click
		public void RequestData()
		{
			StartCoroutine ("requestData");
		}

		private IEnumerator requestData()
		{
			foreach (LeaderboardItem item in _leaderboardItems)
			{
				DestroyImmediate(item.gameObject);
			}
			_leaderboardItems.Clear();

			// get data
			string url = string.Format("https://{0}-{1}.c9.io/leaderboard?limit=10&sort=score%20DESC%20createdAt%20ASC", _C9ProjectName, _C9Username);
			WWW www = new WWW(url);
			yield return www;
			if (null != www.error)
			{
				www.Dispose ();
				yield break;
			}
			//Debug.Log(www.text);
			string jsonData = www.text;
			www.Dispose();

			LeaderboardData[] results = JsonMapper.ToObject<LeaderboardData[]>(jsonData);
			int index = 1;
			foreach (LeaderboardData result in results)
			{
				if (string.IsNullOrEmpty(result.name) ||
				    string.IsNullOrEmpty(result.score))
				{
					continue;
				}
				LeaderboardItem newItem = (LeaderboardItem)Instantiate(_PrefabLeaderboardItem);
				newItem.transform.SetParent(_LeaderboardLabels.transform);
				_leaderboardItems.Add(newItem);
				RectTransform rt = newItem.GetComponent<RectTransform>();
				rt.offsetMin = new Vector2(0, 0);
				rt.offsetMax = new Vector2(0, -2000*(index));
				newItem._Name.text = result.name;
				newItem._Score.text = result.score;
				++index;
			}
		}
	}
}
