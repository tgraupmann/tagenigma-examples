using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace LeaderboardExample
{
	public class LeaderboardManager : MonoBehaviour
	{
		// Singleton instance
		public static LeaderboardManager _Instance = null;

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

		// the name input field
		public Text _InputName = null;

		// the score input field
		public Text _InputScore = null;

		// implements MonoBehaviour
		public void Awake()
		{
			_Instance = this;
		}

		// delete leaderboard record using id
		public void DeleteData(int id)
		{
			StartCoroutine ("deleteData", id);
		}
		public IEnumerator deleteData(int id)
		{
			// get data
			string url = string.Format("https://{0}-{1}.c9.io/leaderboard/destroy?id={2}", _C9ProjectName, _C9Username, id);
			WWW www = new WWW(url);
			yield return www;
			if (null != www.error)
			{
				www.Dispose ();
				yield break;
			}
			string jsonData = www.text;
			www.Dispose();
			Debug.Log(jsonData);

			RequestData ();
		}

		// call from button click
		public void RequestData()
		{
			StartCoroutine ("requestData");
		}
		public IEnumerator requestData()
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

			string jsonData = www.text;
			www.Dispose();
			//Debug.Log(jsonData);

			LeaderboardData[] results = JsonMapper.ToObject<LeaderboardData[]>(jsonData);
			int index = 1;
			foreach (LeaderboardData result in results)
			{
				if (string.IsNullOrEmpty(result.name))
				{
					continue;
				}
				LeaderboardItem newItem = (LeaderboardItem)Instantiate(_PrefabLeaderboardItem);
				newItem.transform.SetParent(_LeaderboardLabels.transform);
				_leaderboardItems.Add(newItem);
				RectTransform rt = newItem.GetComponent<RectTransform>();
				rt.offsetMin = new Vector2(0, 0);
				rt.offsetMax = new Vector2(0, -2000*(index));
				newItem._Id = result.id;
				newItem._Name.text = result.name;
				newItem._Score.text = result.score.ToString();
				++index;
			}
		}

		// call from button click
		public void CreateData()
		{
			StartCoroutine ("createData");
		}
		public IEnumerator createData()
		{
			if (string.IsNullOrEmpty(_InputName.text) ||
			    string.IsNullOrEmpty(_InputScore.text))
			{
				yield break;
			}

			// get data
			string url = string.Format("https://{0}-{1}.c9.io/leaderboard/create?name={2}&score={3}", _C9ProjectName, _C9Username, _InputName.text, _InputScore.text);
			WWW www = new WWW(url);
			yield return www;
			if (null != www.error)
			{
				www.Dispose ();
				yield break;
			}
			
			string jsonData = www.text;
			www.Dispose();
			Debug.Log(jsonData);
			
			RequestData();
		}
	}
}
