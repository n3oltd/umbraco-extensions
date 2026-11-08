import { UuiButton } from '@n3oltd/backoffice-ui';
import type { AuthFetch } from '@n3oltd/backoffice-core';
import { CampaignTagRow } from './campaign-tag-row';
import { getKeyOptions, type CampaignTag } from './campaign-tags-value';
import { useTagKeys } from './use-tag-keys';
import styles from './campaign-tags-app.css?inline';

interface CampaignTagsAppProps {
    rows: CampaignTag[];
    authFetch: AuthFetch | null;
    onChange: (rows: CampaignTag[]) => void;
}

export function CampaignTagsApp({ rows, authFetch, onChange }: CampaignTagsAppProps) {
    const { tagKeys, status, retry } = useTagKeys(authFetch);

    const updateRow = (index: number, row: CampaignTag): void => {
        onChange(rows.map((existing, rowIndex) => (rowIndex === index ? row : existing)));
    };

    const removeRow = (index: number): void => {
        onChange(rows.filter((_, rowIndex) => rowIndex !== index));
    };

    const addRow = (): void => {
        onChange([...rows, { key: '', value: '' }]);
    };

    return (
        <div className="n3o-campaign-tags">
            {status === 'loading' ? <p className="status">Loading tags…</p> : null}

            {status === 'failed' ? (
                <div className="status">
                    <p className="error">The tags could not be loaded.</p>
                    <UuiButton label="Retry" look="secondary" compact onClick={retry} />
                </div>
            ) : null}

            {status === 'loaded' ? (
                <>
                    {rows.map((row, index) => (
                        <CampaignTagRow
                            key={index}
                            row={row}
                            keyOptions={getKeyOptions(tagKeys, rows, index)}
                            authFetch={authFetch}
                            onChange={(changed) => updateRow(index, changed)}
                            onRemove={() => removeRow(index)}
                        />
                    ))}
                    <UuiButton
                        label="Add tag"
                        icon="icon-add"
                        look="placeholder"
                        disabled={getKeyOptions(tagKeys, rows, rows.length).length === 0}
                        onClick={addRow}
                    />
                </>
            ) : null}

            <style>{styles}</style>
        </div>
    );
}
