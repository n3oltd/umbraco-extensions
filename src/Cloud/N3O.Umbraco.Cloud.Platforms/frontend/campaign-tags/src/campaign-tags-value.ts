export interface CampaignTag {
    key: string;
    value: string;
}

export interface TagKey {
    key: string;
    name: string;
    description?: string;
}

export function toRows(value: CampaignTag[] | undefined): CampaignTag[] {
    return value?.map((tag) => ({ key: tag.key, value: tag.value })) ?? [];
}

export function toValue(rows: CampaignTag[]): CampaignTag[] | undefined {
    const tags = rows
        .filter((row) => row.key && row.value.trim())
        .map((row) => ({ key: row.key, value: row.value.trim() }));

    return tags.length ? tags : undefined;
}

export function isSameValue(rows: CampaignTag[], value: CampaignTag[] | undefined): boolean {
    return JSON.stringify(toValue(rows) ?? []) === JSON.stringify(toValue(toRows(value)) ?? []);
}

export function getKeyOptions(tagKeys: TagKey[], rows: CampaignTag[], index: number): TagKey[] {
    const takenKeys = rows.filter((_, rowIndex) => rowIndex !== index).map((row) => row.key);
    const options = tagKeys.filter((tagKey) => !takenKeys.includes(tagKey.key));
    const key = rows[index]?.key;

    if (key && !tagKeys.some((tagKey) => tagKey.key === key)) {
        return [...options, { key, name: key }];
    }

    return options;
}
