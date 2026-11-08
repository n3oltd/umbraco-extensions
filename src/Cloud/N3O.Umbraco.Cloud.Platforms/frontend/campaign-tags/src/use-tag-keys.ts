import { useEffect, useState } from 'react';
import type { AuthFetch } from '@n3oltd/backoffice-core';
import type { TagKey } from './campaign-tags-value';

export type TagKeysStatus = 'loading' | 'loaded' | 'failed';

export interface TagKeys {
    tagKeys: TagKey[];
    status: TagKeysStatus;
    retry: () => void;
}

export function useTagKeys(authFetch: AuthFetch | null): TagKeys {
    const [tagKeys, setTagKeys] = useState<TagKey[]>([]);
    const [status, setStatus] = useState<TagKeysStatus>('loading');
    const [attempt, setAttempt] = useState(0);

    useEffect(() => {
        if (!authFetch) {
            return;
        }

        const controller = new AbortController();

        const load = async (): Promise<void> => {
            setStatus('loading');

            try {
                const res = await authFetch('/umbraco/backoffice/api/PlatformsBackOffice/tags/keys', {
                    headers: { Accept: 'application/json' },
                    signal: controller.signal,
                });

                if (!res.ok) {
                    setStatus('failed');
                    return;
                }

                setTagKeys((await res.json()) as TagKey[]);
                setStatus('loaded');
            } catch (err) {
                if (!controller.signal.aborted) {
                    console.error('[CampaignTags] Failed to load tag keys:', err);
                    setStatus('failed');
                }
            }
        };

        void load();

        return () => controller.abort();
    }, [authFetch, attempt]);

    return { tagKeys, status, retry: () => setAttempt((value) => value + 1) };
}
