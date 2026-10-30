import { useCallback, useEffect, useState } from 'react';

/** The part after "#", e.g. "/lektioner/trumf". Hash routes need no server setup and keep the back button working. */
const current = () => window.location.hash.replace(/^#/, '') || '/';

export function useRoute(): [string, (path: string) => void] {
  const [path, setPath] = useState(current);
  useEffect(() => {
    const onChange = () => {
      setPath(current());
      window.scrollTo(0, 0);
    };
    window.addEventListener('hashchange', onChange);
    return () => window.removeEventListener('hashchange', onChange);
  }, []);
  const navigate = useCallback((to: string) => {
    window.location.hash = to;
  }, []);
  return [path, navigate];
}
