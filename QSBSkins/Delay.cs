﻿using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace QSBSkins;

/// <summary>
/// Taken from New Horizons
/// </summary>
public static class Delay
{
	#region OnSceneUnloaded
	static Delay() => SceneManager.sceneUnloaded += OnSceneUnloaded;

	private static void OnSceneUnloaded(Scene _) => QSBSkins.Instance.StopAllCoroutines();
	#endregion

	#region public methods
	public static void StartCoroutine(IEnumerator coroutine) => QSBSkins.Instance.StartCoroutine(coroutine);

	public static void RunWhen(Func<bool> predicate, Action action) => StartCoroutine(RunWhenCoroutine(action, predicate));

	public static void FireInNUpdates(Action action, int n) => StartCoroutine(FireInNUpdatesCoroutine(action, n));

	public static void FireOnNextUpdate(Action action) => FireInNUpdates(action, 1);

	public static void RunWhenAndInNUpdates(Action action, Func<bool> predicate, int n) => Delay.StartCoroutine(RunWhenOrInNUpdatesCoroutine(action, predicate, n));
	#endregion

	#region Coroutines
	private static IEnumerator RunWhenCoroutine(Action action, Func<bool> predicate)
	{
		while (!predicate.Invoke())
		{
			yield return new WaitForEndOfFrame();
		}

		action.Invoke();
	}

	private static IEnumerator FireInNUpdatesCoroutine(Action action, int n)
	{
		for (int i = 0; i < n; i++)
		{
			yield return new WaitForEndOfFrame();
		}
		action?.Invoke();
	}

	private static IEnumerator RunWhenOrInNUpdatesCoroutine(Action action, Func<bool> predicate, int n)
	{
		for (int i = 0; i < n; i++)
		{
			yield return new WaitForEndOfFrame();
		}
		while (!predicate.Invoke())
		{
			yield return new WaitForEndOfFrame();
		}

		action.Invoke();
	}
	#endregion
}