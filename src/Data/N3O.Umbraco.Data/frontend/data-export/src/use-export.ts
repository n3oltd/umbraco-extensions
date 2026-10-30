import { useEffect, useRef, useState } from 'react';
import { downloadResponse, type AuthFetch } from '@n3oltd/backoffice-core';
import type { ContentType, ContentMetadata, ExportProgressResponse, CreateExportResponse, Notify } from './types';

export interface ExportServerData {
    contentTypes: ContentType[];
    metadatas: ContentMetadata[];
}

export function useExportServerData(
    contentKey: string | null,
    authFetch: AuthFetch | null,
): ExportServerData {
    const [contentTypes, setContentTypes] = useState<ContentType[]>([]);
    const [metadatas, setMetadatas] = useState<ContentMetadata[]>([]);

    useEffect(() => {
        if (!contentKey || !authFetch) {
            return;
        }

        let active = true;

        const load = async (): Promise<void> => {
            const [typesRes, metaRes] = await Promise.all([
                authFetch(`/umbraco/backoffice/api/ContentTypes/${contentKey}/relations?type=descendant`, {
                    headers: { Accept: 'application/json' },
                }),
                authFetch('/umbraco/backoffice/api/Exports/lookups/contentMetadata', {
                    headers: { Accept: 'application/json' },
                }),
            ]);

            const types = (await typesRes.json()) as ContentType[];
            const metadata = (await metaRes.json()) as ContentMetadata[];

            for (const m of metadata) {
                m.selected = m.autoSelected;
            }

            metadata.sort((a, b) => a.displayOrder - b.displayOrder);

            if (active) {
                setContentTypes(types);
                setMetadatas(metadata);
            }
        };

        void load();

        return () => {
            active = false;
        };
    }, [contentKey, authFetch]);

    return { contentTypes, metadatas };
}

export interface ExportRun {
    processing: boolean;
    progress: string;
    doExport: (
        contentKey: string | null,
        contentTypeAlias: string,
        format: string,
        includeUnpublished: boolean,
        selectedMetadataIds: string[],
        selectedPropertyAliases: string[],
    ) => Promise<void>;
}

export function useExportRun(authFetch: AuthFetch | null, notify: Notify): ExportRun {
    const [processing, setProcessing] = useState<boolean>(false);
    const [progress, setProgress] = useState<string>('');

    const generationRef = useRef<number>(0);
    const pollTimerRef = useRef<ReturnType<typeof setTimeout> | undefined>(undefined);

    useEffect(() => {
        return () => {
            clearTimeout(pollTimerRef.current);
            generationRef.current += 1;
        };
    }, []);

    const processingError = (message: string): void => {
        setProcessing(false);
        setProgress('');
        notify('danger', 'Export failed', message);
    };

    const poll = (exportId: string): Promise<ExportProgressResponse> => {
        const gen = generationRef.current;

        const executePoll = async (
            resolve: (value: ExportProgressResponse) => void,
            reject: (reason?: unknown) => void
        ): Promise<void> => {
            if (generationRef.current !== gen) {
                reject(new Error('poll cancelled'));
                return;
            }

            const getProgress = await authFetch!(`/umbraco/backoffice/api/Exports/export/${exportId}/progress`, {
                headers: { Accept: 'application/json', 'Content-Type': 'application/json' },
                method: 'GET',
            });

            if (!getProgress.ok) {
                const progressRes = (await getProgress.json()) as ExportProgressResponse;
                processingError(String(progressRes));
                reject(progressRes);
                return;
            }

            const progressRes = (await getProgress.json()) as ExportProgressResponse;

            if (progressRes.isComplete === true) {
                resolve(progressRes);
            } else {
                setProgress(progressRes.text);
                
                pollTimerRef.current = setTimeout(() => void executePoll(resolve, reject), 2500);
            }
        };

        return new Promise(executePoll);
    };

    const doExport = async (
        contentKey: string | null,
        contentTypeAlias: string,
        format: string,
        includeUnpublished: boolean,
        selectedMetadataIds: string[],
        selectedPropertyAliases: string[],
    ): Promise<void> => {
        clearTimeout(pollTimerRef.current);
        generationRef.current += 1;

        setProcessing(true);
        setProgress('');

        if (!selectedPropertyAliases.length && !selectedMetadataIds.length) {
            processingError('At least one property or metadata field must be selected');
            return;
        }

        const req = {
            format,
            includeUnpublished,
            metadata: selectedMetadataIds,
            properties: selectedPropertyAliases,
        };

        const createExport = await authFetch!(
            `/umbraco/backoffice/api/Exports/export/${contentKey}/${contentTypeAlias}`,
            {
                headers: { Accept: 'application/json', 'Content-Type': 'application/json' },
                method: 'POST',
                body: JSON.stringify(req),
            }
        );

        if (!createExport.ok) {
            const createRes = (await createExport.json()) as CreateExportResponse;
            processingError(String(createRes));
            return;
        }

        const createRes = (await createExport.json()) as CreateExportResponse;

        poll(createRes.id)
            .then(async (res) => {
                const exportFile = await authFetch!(`/umbraco/backoffice/api/Exports/export/${res.id}/file`, {
                    headers: { Accept: 'application/json', 'Content-Type': 'application/json' },
                    method: 'GET',
                });

                if (!exportFile.ok) {
                    processingError(String(await exportFile.json()));
                    return;
                }

                await downloadResponse(exportFile, 'export');

                setProcessing(false);
                setProgress('');
            })
            .catch(() => { });
    };

    return { processing, progress, doExport };
}
