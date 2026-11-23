using System;
using Anomaly.Model.Story;
using Anomaly.View.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Anomaly.Presenter.Lobby
{
    // 로비: 화면이 밝아지면 메뉴 사용 가능 → 게임 시작 → 암전
    //   → (인트로 스토리가 있고 아직 안 봤으면) 스토리 한 줄씩 → 암전 → 게임 씬(0번 맵 복도) 로드
    public sealed class LobbyPresenter : IDisposable
    {
        // 인트로를 본 적 있는지 저장하는 키(PlayerPrefs). 다시 보고 싶으면 이 키를 지우면 됨
        public const string StorySeenKey = "Anomaly.IntroStorySeen";

        private readonly LobbyMenuView menu;
        private readonly ScreenFadeView fade;
        private readonly StoryView storyView;
        private readonly StoryData story;
        private readonly string gameSceneName;
        private bool starting;
        private bool finishing;
        private int lineIndex;

        public LobbyPresenter(LobbyMenuView menu, ScreenFadeView fade, StoryView storyView, StoryData story, string gameSceneName)
        {
            this.menu = menu;
            this.fade = fade;
            this.storyView = storyView;
            this.story = story;
            this.gameSceneName = gameSceneName;
            menu.StartClicked += OnStartClicked;
            menu.QuitClicked += OnQuitClicked;
            if (storyView != null)
            {
                storyView.Advanced += OnStoryAdvanced;
                storyView.Skipped += FinishStory;
            }
        }

        public void Dispose()
        {
            menu.StartClicked -= OnStartClicked;
            menu.QuitClicked -= OnQuitClicked;
            if (storyView != null)
            {
                storyView.Advanced -= OnStoryAdvanced;
                storyView.Skipped -= FinishStory;
            }
        }

        public void Begin()
        {
            menu.SetInteractable(false);
            if (fade == null)
            {
                EnableMenu();
                return;
            }
            fade.SetCovered(ScreenFadeKind.Plain);
            fade.FadeIn(EnableMenu);
        }

        private void EnableMenu()
        {
            menu.SetInteractable(true);
            menu.FocusStart();
        }

        private void OnStartClicked()
        {
            if (starting) return; // 연타 방지
            starting = true;
            menu.SetInteractable(false);
            if (ShouldPlayStory()) Cover(BeginStory);
            else Cover(LoadGame);
        }

        private bool ShouldPlayStory()
        {
            if (storyView == null || story == null || story.Lines.Count == 0) return false;
            return !story.ShowOnlyFirstTime || PlayerPrefs.GetInt(StorySeenKey, 0) == 0;
        }

        // ---------- 인트로 스토리 ----------
        private void BeginStory()
        {
            lineIndex = 0;
            storyView.Show();
            storyView.SetInteractable(false);
            storyView.ShowLine(story.Lines[0]);
            if (fade != null) fade.FadeIn(() => storyView.SetInteractable(true));
            else storyView.SetInteractable(true);
        }

        private void OnStoryAdvanced()
        {
            if (finishing) return;
            lineIndex++;
            if (lineIndex < story.Lines.Count) storyView.ShowLine(story.Lines[lineIndex]);
            else FinishStory();
        }

        private void FinishStory()
        {
            if (finishing) return;
            finishing = true;
            storyView.SetInteractable(false);
            PlayerPrefs.SetInt(StorySeenKey, 1);
            PlayerPrefs.Save();
            Cover(LoadGame);
        }

        private void Cover(Action then)
        {
            if (fade != null) fade.FadeOut(ScreenFadeKind.Plain, then);
            else then();
        }

        private void LoadGame()
        {
            SceneManager.LoadScene(gameSceneName);
        }

        private void OnQuitClicked()
        {
#if UNITY_EDITOR
            Debug.Log("[LobbyPresenter] 종료(에디터에서는 플레이 모드가 유지됨)");
#endif
            Application.Quit();
        }
    }
}
