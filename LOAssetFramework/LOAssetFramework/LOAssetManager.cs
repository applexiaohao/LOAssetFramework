using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace LOAssetFramework
{	
	public class LOAssetManager:MonoBehaviour
	{

		#region 单例模式
		private static GameObject 		s_go_LOAssetManager = null;
		private static LOAssetManager	s_LOAssetManager = null;

		public static LOAssetManager DefaultAssetManager{
			get{
				if (s_LOAssetManager == null) {
					s_go_LOAssetManager = new GameObject ("LOAssetManager");
					s_LOAssetManager = s_go_LOAssetManager.AddComponent<LOAssetManager> ();
				}

				return s_LOAssetManager;
			}
		}

		#endregion

		public 	static string URI{ set; get;}
		public static string ManifestName{ set; get;}
		public static Action<bool> InitBlock;

		public AssetBundleManifest manifest{ set; get;}

		/// <summary>
		/// 
		/// </summary>
		/// <returns>The asset.</returns>
		public T GetAsset<T>(string assetbundlename,string assetname) where T:UnityEngine.Object
		{
			LOAssetBundle lab = LOAssetCache.GetBundleCache(assetbundlename);

			if (lab == null) {
				return null;
			}
			else
			{
				return lab.m_AssetBundle.LoadAsset<T>(assetname);
			}
		}

		IEnumerator LoadManifestBundle()
		{
			if (LOAssetCache.InCache(LOAssetManager.ManifestName)) {
				yield break;
			}

			// 通过网络下载AssetBundle
			WWW www = IsLoadAssetBundleAtInternal(LOAssetManager.ManifestName);
			yield return www;

			this.manifest = this.GetAsset<AssetBundleManifest>(LOAssetManager.ManifestName,"AssetBundleManifest");
			LOAssetManager.InitBlock (this.manifest != null);
		}
		void Start()
		{
			StartCoroutine ("LoadManifestBundle");
		}




		#region 加载包裹系列函数

		/// <summary>
		/// 检查是否已经从网络下载
		/// </summary>
		protected WWW IsLoadAssetBundleAtInternal (string assetBundleName)
		{
			//已经存在了呀
			LOAssetBundle bundle = LOAssetCache.GetBundleCache(assetBundleName);

			if (bundle != null)
			{
				//保留一次
				bundle.Retain ();
				return null;
			}

			//如果WWW缓存策略中包含有对应的关键字,则返回null
			if (LOAssetCache.InWWWCache (assetBundleName)) {
				return null;
			}
			//创建下载链接
			WWW www = new WWW(LOAssetManager.URI + assetBundleName);
			//加入缓存策略
			LOAssetCache.SetWWWCache(assetBundleName,www);

			return www;
		}


		IEnumerator LoadDependencies(string assetBundleName)
		{
			if (this.manifest == null) {
				yield break;
			}
			// 获取依赖包裹
			string[] dependencies = this.manifest.GetAllDependencies(assetBundleName);

			if (dependencies.Length == 0)
			{
				yield break;
			}

			// 记录并且加载所有的依赖包裹
			LOAssetCache.SetDependCache(assetBundleName, dependencies);

			for (int i = 0; i < dependencies.Length; i++) 
			{
				yield return IsLoadAssetBundleAtInternal (dependencies [i]);
			}
		}

		/// <summary>
		/// 加载资源包
		/// </summary>
		IEnumerator LoadAssetBundle(string assetBundleName)
		{
			if (LOAssetCache.InCache(assetBundleName)) {
				yield break;
			}
			// 通过网络下载AssetBundle
			WWW www = IsLoadAssetBundleAtInternal(assetBundleName);
			yield return www;

			// 通过网络加载失败，下载依赖包裹
			yield return StartCoroutine(LoadDependencies(assetBundleName));
		}

		/// <summary>
		/// 异步加载资源
		/// </summary>
		public IEnumerator LoadAssetAsync (string assetBundleName)
		{
			//开始加载包裹
			yield return StartCoroutine(LoadAssetBundle (assetBundleName));
		}

		/// <summary>
		/// 异步加载场景
		/// </summary>
		public IEnumerator LoadLevelAsync (string assetBundleName)
		{
			//加载资源包
			yield return StartCoroutine(LoadAssetBundle (assetBundleName));

		}
		#endregion


		#region Update

		void Update()
		{
			LOAssetCache.Update();
		}
		#endregion
	}
}

