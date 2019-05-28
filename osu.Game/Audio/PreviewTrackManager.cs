﻿// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Audio;
using osu.Framework.Audio.Track;
using osu.Framework.Bindables;
using osu.Framework.Configuration;
using osu.Framework.Graphics;
using osu.Framework.IO.Stores;
using osu.Game.Beatmaps;

namespace osu.Game.Audio
{
    /// <summary>
    /// A central store for the retrieval of <see cref="PreviewTrack"/>s.
    /// </summary>
    public class PreviewTrackManager : Component
    {
        private readonly BindableDouble muteBindable = new BindableDouble();

        private AudioManager audio;
        private IAdjustableResourceStore<Track> trackStore;

        private TrackManagerPreviewTrack current;

        [BackgroundDependencyLoader]
        private void load(AudioManager audio, FrameworkConfigManager config)
        {
            trackStore = audio.GetTrackStore(new OnlineStore());

            this.audio = audio;

            config.BindWith(FrameworkSetting.VolumeMusic, trackStore.Volume);
        }

        /// <summary>
        /// Retrieves a <see cref="PreviewTrack"/> for a <see cref="BeatmapSetInfo"/>.
        /// </summary>
        /// <param name="beatmapSetInfo">The <see cref="BeatmapSetInfo"/> to retrieve the preview track for.</param>
        /// <returns>The playable <see cref="PreviewTrack"/>.</returns>
        public PreviewTrack Get(BeatmapSetInfo beatmapSetInfo)
        {
            var track = CreatePreviewTrack(beatmapSetInfo, trackStore);

            track.Started += () =>
            {
                current?.Stop();
                current = track;
                audio.Tracks.AddAdjustment(AdjustableProperty.Volume, muteBindable);
            };

            track.Stopped += () =>
            {
                current = null;
                audio.Tracks.RemoveAdjustment(AdjustableProperty.Volume, muteBindable);
            };

            return track;
        }

        /// <summary>
        /// Stops any currently playing <see cref="PreviewTrack"/>.
        /// </summary>
        /// <remarks>
        /// Only the immediate owner (an object that implements <see cref="IPreviewTrackOwner"/>) of the playing <see cref="PreviewTrack"/>
        /// can globally stop the currently playing <see cref="PreviewTrack"/>. The object holding a reference to the <see cref="PreviewTrack"/>
        /// can always stop the <see cref="PreviewTrack"/> themselves through <see cref="PreviewTrack.Stop()"/>.
        /// </remarks>
        /// <param name="source">The <see cref="IPreviewTrackOwner"/> which may be the owner of the <see cref="PreviewTrack"/>.</param>
        public void StopAnyPlaying(IPreviewTrackOwner source)
        {
            if (current == null || current.Owner != source)
                return;

            current.Stop();
            current = null;
        }

        /// <summary>
        /// Creates the <see cref="TrackManagerPreviewTrack"/>.
        /// </summary>
        protected virtual TrackManagerPreviewTrack CreatePreviewTrack(BeatmapSetInfo beatmapSetInfo, IResourceStore<Track> trackStore) => new TrackManagerPreviewTrack(beatmapSetInfo, trackStore);

        protected class TrackManagerPreviewTrack : PreviewTrack
        {
            public IPreviewTrackOwner Owner { get; private set; }

            private readonly BeatmapSetInfo beatmapSetInfo;
            private readonly IResourceStore<Track> trackManager;

            public TrackManagerPreviewTrack(BeatmapSetInfo beatmapSetInfo, IResourceStore<Track> trackManager)
            {
                this.beatmapSetInfo = beatmapSetInfo;
                this.trackManager = trackManager;
            }

            [BackgroundDependencyLoader]
            private void load(IPreviewTrackOwner owner)
            {
                Owner = owner;
            }

            protected override Track GetTrack() => trackManager.Get($"https://b.ppy.sh/preview/{beatmapSetInfo?.OnlineBeatmapSetID}.mp3");
        }
    }
}
