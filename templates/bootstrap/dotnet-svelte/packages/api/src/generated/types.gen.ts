// This file is generated from the published OpenAPI document. Do not edit it by hand.

export type RecordCheckInData = {
  body: {
    note?: string | null;
  };
};

export type RecordCheckInResponse = {
  checkInId: string;
  state: string;
};

export type CheckInRow = {
  checkInId: string;
  recordedAt: string;
  note?: string | null;
  state: string;
};

export type ListCheckInsResponse = {
  checkIns: CheckInRow[];
};

export type ReadCheckInData = {
  path: {
    checkInId: string;
  };
};

export type ReadCheckInResponse = {
  checkInId: string;
  recordedAt: string;
  note?: string | null;
  state: string;
};
