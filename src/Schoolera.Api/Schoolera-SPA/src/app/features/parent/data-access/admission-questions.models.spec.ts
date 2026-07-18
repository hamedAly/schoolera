import { describe, expect, it } from 'vitest';

import {
  AdmissionQuestionType,
  selectedOptionLabels,
} from './admission-questions.models';

describe('admission-questions.models', () => {
  it('maps selected option codes to localized labels', () => {
    const labels = selectedOptionLabels({
      selectedOptionCodes: ['a', 'b'],
      options: [
        { optionCode: 'a', label: 'Alpha' },
        { optionCode: 'b', label: 'Beta' },
        { optionCode: 'c', label: 'Gamma' },
      ],
    });

    expect(labels).toEqual(['Alpha', 'Beta']);
  });

  it('exposes stable question type constants', () => {
    expect(AdmissionQuestionType.YesNo).toBe(6);
  });
});
