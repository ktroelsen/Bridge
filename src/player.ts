import { useCallback, useEffect, useState } from 'react';
import { fetchProgress, registerPlayer, saveLessonResult } from './api';

/** Best score (0-100) per finished lesson. */
export type Progress = Record<string, number>;

const NAME_KEY = 'bridge.player';
const PROGRESS_KEY = 'bridge.progress';

// localStorage can be missing or throw (private mode); then progress only lasts the session.
function read<T>(key: string): T | null {
  try {
    const raw = localStorage.getItem(key);
    return raw ? (JSON.parse(raw) as T) : null;
  } catch {
    return null;
  }
}

function write(key: string, value: unknown) {
  try {
    if (value === null) localStorage.removeItem(key);
    else localStorage.setItem(key, JSON.stringify(value));
  } catch {
    /* not saved */
  }
}

const merge = (a: Progress, b: Progress): Progress => {
  const out = { ...a };
  for (const [id, score] of Object.entries(b)) out[id] = Math.max(out[id] ?? 0, score);
  return out;
};

/**
 * The player (just a name, no login) and their lesson progress. Progress is kept in the browser
 * too, so it works without a name and without network; with a name it is also saved on the
 * server and follows the player to other devices.
 */
export function usePlayer() {
  const [name, setNameState] = useState<string | null>(() => read<string>(NAME_KEY));
  const [progress, setProgress] = useState<Progress>(() => read<Progress>(PROGRESS_KEY) ?? {});
  // Whether the player chose to go on without a name (so we don't keep asking).
  const [skipped, setSkipped] = useState(() => read<boolean>('bridge.skipName') ?? false);

  const update = useCallback((next: (p: Progress) => Progress) => {
    setProgress((p) => {
      const value = next(p);
      write(PROGRESS_KEY, value);
      return value;
    });
  }, []);

  // Fetch the server's progress for the player (on start and when the name changes).
  useEffect(() => {
    if (!name) return;
    fetchProgress(name)
      .then((rows) => update((p) => merge(p, Object.fromEntries(rows.map((r) => [r.lessonId, r.bestScore])))))
      .catch(() => {
        /* offline: the local copy is used */
      });
  }, [name, update]);

  const setName = useCallback(
    async (raw: string) => {
      const player = await registerPlayer(raw);
      write(NAME_KEY, player.name);
      setNameState(player.name);
      // Lessons done before the name was given are saved for the player too.
      const local = read<Progress>(PROGRESS_KEY) ?? {};
      await Promise.all(Object.entries(local).map(([id, score]) => saveLessonResult(player.name, id, score).catch(() => null)));
    },
    [],
  );

  const forgetName = useCallback(() => {
    write(NAME_KEY, null);
    write(PROGRESS_KEY, null);
    write('bridge.skipName', null);
    setNameState(null);
    setProgress({});
    setSkipped(false);
  }, []);

  const skipName = useCallback(() => {
    write('bridge.skipName', true);
    setSkipped(true);
  }, []);

  const recordLesson = useCallback(
    (lessonId: string, score: number) => {
      update((p) => merge(p, { [lessonId]: score }));
      if (name) saveLessonResult(name, lessonId, score).catch(() => null);
    },
    [name, update],
  );

  return { name, progress, askName: !name && !skipped, setName, forgetName, skipName, recordLesson };
}

export type PlayerState = ReturnType<typeof usePlayer>;
