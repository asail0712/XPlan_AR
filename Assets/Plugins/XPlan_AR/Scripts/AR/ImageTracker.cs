using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if !VUFORIA_IOS_SETTINGS && !VUFORIA_ANDROIDS_SETTINGS
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
#else
using Vuforia;
#endif //AR_FOUNDATION

// 參考資料
// https://www.youtube.com/watch?v=Fpw7V3oa4fs

namespace XPlan.AR
{
	public class ImageTracker : MonoBehaviour
	{
		[SerializeField] Vector3 relativePos;

#if !VUFORIA_IOS_SETTINGS && !VUFORIA_ANDROIDS_SETTINGS
		[SerializeField] private ARTrackedImageManager trackedImageMgr;
#else
		[SerializeField] private DefaultObserverEventHandler observerEventHandler;

		private Coroutine trackCoroutine	= null;
		private bool bTargetFound			= false;
#endif

		private void OnEnable()
		{
#if !VUFORIA_IOS_SETTINGS && !VUFORIA_ANDROIDS_SETTINGS
			trackedImageMgr.trackedImagesChanged += OnTrakedImgChanged;
#else
			if (observerEventHandler == null)
			{
				return;
			}

			ObserverBehaviour mObserverBehaviour = observerEventHandler.GetComponent<ObserverBehaviour>();

			if (mObserverBehaviour == null)
			{
				return;
			}

			// mObserverBehaviour
			mObserverBehaviour.enabled = true;
			observerEventHandler.OnTargetFound.AddListener(OnTargetFound);
			observerEventHandler.OnTargetLost.AddListener(OnTargetLost);
#endif
		}

		private void OnDisable()
		{
#if !VUFORIA_IOS_SETTINGS && !VUFORIA_ANDROIDS_SETTINGS
			trackedImageMgr.trackedImagesChanged -= OnTrakedImgChanged;
#else
			if (trackCoroutine != null)
			{
				StopCoroutine(trackCoroutine);
			}

			if (observerEventHandler == null)
			{
				return;
			}

			ObserverBehaviour mObserverBehaviour = observerEventHandler.GetComponent<ObserverBehaviour>();

			if (mObserverBehaviour == null)
			{
				return;
			}

			// mObserverBehaviour
			mObserverBehaviour.enabled = false;
			observerEventHandler.OnTargetFound.RemoveAllListeners();
			observerEventHandler.OnTargetLost.RemoveAllListeners();
#endif
		}

#if !VUFORIA_IOS_SETTINGS && !VUFORIA_ANDROIDS_SETTINGS
		private void OnTrakedImgChanged(ARTrackedImagesChangedEventArgs eventArgs)
		{
			foreach (ARTrackedImage imgTracker in eventArgs.added)
			{
				string imgKey			= imgTracker.referenceImage.name;
				Vector3 spawnPos		= imgTracker.transform.position;
				XARModelSpawnMsg msg	= new XARModelSpawnMsg(imgKey, spawnPos);

				msg.Send();
			}

			foreach (ARTrackedImage imgTracker in eventArgs.updated)
			{
				string imgKey			= imgTracker.referenceImage.name;
				bool bOn				= imgTracker.trackingState == TrackingState.Tracking;
				Vector3 spawnPos		= imgTracker.transform.position;
				XARModelTrackMsg msg	= new XARModelTrackMsg(imgKey, bOn, spawnPos);

				msg.Send();
			}
		}
#else
		private void OnTargetFound()
		{
			ObserverBehaviour mObserverBehaviour = observerEventHandler.GetComponent<ObserverBehaviour>();

			if (mObserverBehaviour == null)
			{
				return;
			}

			string targetKey	= mObserverBehaviour.TargetName;
			GameObject go		= mObserverBehaviour.gameObject;
			bTargetFound		= true;

			if(trackCoroutine != null)
			{
				StopCoroutine(trackCoroutine);
			}

			trackCoroutine = StartCoroutine(TrackARModel(targetKey, go));
		}

		private void OnTargetLost()
		{
			bTargetFound = false;
		}

		private IEnumerator TrackARModel(string targetKey, GameObject go)
		{
			do
			{
				yield return new WaitForSeconds(0.1f);

				Debug.Log($"{targetKey} => {go.transform.position}");

				string imgKey			= targetKey;
				bool bOn				= bTargetFound;
				Vector3 spawnPos		= go.transform.position + relativePos;

				XARModelTrackMsg msg	= new XARModelTrackMsg(imgKey, bOn, spawnPos);
				msg.Send();

			} while (bTargetFound);
		}
#endif //AR_FOUNDATION
	}
}
