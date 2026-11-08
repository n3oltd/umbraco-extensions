import { useEffect, useState } from 'react';
import type { AuthFetch } from '@n3oltd/backoffice-core';

const SEARCH_DELAY_MS = 300;
const REFUSED_MESSAGE = 'Value suggestions are unavailable: the request was refused.';
const FAILED_MESSAGE = 'Value suggestions could not be loaded.';

interface TagValueRes {
    value: string;
}

interface TagValuesResult {
    key: string;
    values: string[];
    error?: string;
}

export interface TagValues {
    values: string[];
    error?: string;
}

export function useTagValues(authFetch: AuthFetch | null, key: string, searchText: string): TagValues {
    const [result, setResult] = useState<TagValuesResult>();
    const search = searchText.trim();

    useEffect(() => {
        if (!authFetch || !key) {
            return;
        }

        const controller = new AbortController();
        const query = new URLSearchParams({ key });

        if (search) {
            query.set('searchText', search);
        }

        const load = async (): Promise<void> => {
            try {
                const res = await authFetch(`/umbraco/backoffice/api/PlatformsBackOffice/tags/values?${query}`, {
                    headers: { Accept: 'application/json' },
                    signal: controller.signal,
                });

                if (res.status === 401) {
                    setResult({ key, values: [], error: REFUSED_MESSAGE });
                } else if (!res.ok) {
                    setResult({ key, values: [], error: FAILED_MESSAGE });
                } else {
                    const tagValues = (await res.json()) as TagValueRes[];

                    setResult({ key, values: tagValues.map((tagValue) => tagValue.value) });
                }
            } catch (err) {
                if (!controller.signal.aborted) {
                    console.error('[CampaignTags] Failed to load tag values:', err);
                    setResult({ key, values: [], error: FAILED_MESSAGE });
                }
            }
        };

        const timeout = window.setTimeout(() => void load(), search ? SEARCH_DELAY_MS : 0);

        return () => {
            window.clearTimeout(timeout);
            controller.abort();
        };
    }, [authFetch, key, search]);

    if (!key || result?.key !== key) {
        return { values: [] };
    }

    return { values: result.values, error: result.error };
}
