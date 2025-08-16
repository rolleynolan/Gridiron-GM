from __future__ import annotations

from typing import Literal

from pydantic import BaseModel, field_validator, model_validator


class ContractYearTerm(BaseModel):
    year: int
    base: int
    signingProrated: int = 0
    rosterBonus: int = 0
    workoutBonus: int = 0
    guaranteedBase: int = 0

    @field_validator(
        "year",
        "base",
        "signingProrated",
        "rosterBonus",
        "workoutBonus",
        "guaranteedBase",
    )
    @classmethod
    def non_negative(cls, v: int, info):
        if v < 0:
            raise ValueError(f"GG-CAP-1001: {info.field_name} must be non-negative")
        return v


class Guarantee(BaseModel):
    type: Literal["full", "injury", "partial"]
    throughYear: int


class Incentive(BaseModel):
    type: Literal["PPI", "LTBE", "NLTBE"]
    amount: int
    metric: str
    threshold: str


class ContractDTO(BaseModel, extra="allow"):
    api_version: Literal["gg.v1"]
    startYear: int
    endYear: int
    terms: list[ContractYearTerm]
    guarantees: list[Guarantee] = []
    incentives: list[Incentive] = []
    flags: dict[str, bool] = {}
    notes: list[str] = []

    @field_validator("api_version")
    @classmethod
    def check_version(cls, v: str) -> str:
        if v != "gg.v1":
            raise ValueError("GG-CAP-1000: api_version must be 'gg.v1'")
        return v

    @model_validator(mode="after")
    def check_years_and_terms(self) -> "ContractDTO":
        if self.endYear < self.startYear:
            raise ValueError("GG-CAP-1002: endYear must be >= startYear")
        expected = list(range(self.startYear, self.endYear + 1))
        years = [t.year for t in self.terms]
        if years != expected:
            raise ValueError("GG-CAP-1003: terms must cover startYear..endYear consecutively")
        return self
